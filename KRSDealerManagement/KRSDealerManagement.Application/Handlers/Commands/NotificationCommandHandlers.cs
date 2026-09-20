using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using MediatR;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateNotificationCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<int> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            var title = request.Title.Trim();
            var body = request.Body.Trim();
            if (string.IsNullOrWhiteSpace(title))
                throw new InvalidOperationException("Title is required.");
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("Message is required.");

            var dealershipIds = await NotificationService.ResolveTargetDealershipIdsAsync(
                _unitOfWork, request.TargetAllLocations, request.DealershipIds);
            if (dealershipIds.Count == 0)
                throw new InvalidOperationException("Select at least one location or choose All locations.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var notificationId = await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    Title = title,
                    Body = body,
                    TargetAllLocations = request.TargetAllLocations,
                    CreatedByUserId = request.CreatedByUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                });

                if (!request.TargetAllLocations)
                    await NotificationService.ReplaceTargetsAsync(_unitOfWork, notificationId, dealershipIds);

                await NotificationService.SyncRecipientsAsync(_unitOfWork, notificationId, dealershipIds);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return notificationId;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }

    public class UpdateNotificationCommandHandler : IRequestHandler<UpdateNotificationCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateNotificationCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null || !notification.IsActive)
                throw new InvalidOperationException("Notification not found.");

            var title = request.Title.Trim();
            var body = request.Body.Trim();
            if (string.IsNullOrWhiteSpace(title))
                throw new InvalidOperationException("Title is required.");
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("Message is required.");

            var dealershipIds = await NotificationService.ResolveTargetDealershipIdsAsync(
                _unitOfWork, request.TargetAllLocations, request.DealershipIds);
            if (dealershipIds.Count == 0)
                throw new InvalidOperationException("Select at least one location or choose All locations.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                notification.Title = title;
                notification.Body = body;
                notification.TargetAllLocations = request.TargetAllLocations;
                notification.ModifiedByUserId = request.ModifiedByUserId;
                notification.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.Notifications.UpdateAsync(notification);

                if (request.TargetAllLocations)
                {
                    var existingTargets = (await _unitOfWork.NotificationTargets.GetAllAsync())
                        .Where(t => t.NotificationId == notification.NotificationId)
                        .ToList();
                    foreach (var target in existingTargets)
                        await _unitOfWork.NotificationTargets.DeleteAsync(target.NotificationTargetId);
                }
                else
                {
                    await NotificationService.ReplaceTargetsAsync(_unitOfWork, notification.NotificationId, dealershipIds);
                }

                await NotificationService.SyncRecipientsAsync(_unitOfWork, notification.NotificationId, dealershipIds);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }

    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteNotificationCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null || !notification.IsActive)
                return false;

            notification.IsActive = false;
            notification.DeletedByUserId = request.DeletedByUserId;
            notification.DeletedDate = DateTime.UtcNow;
            notification.ModifiedByUserId = request.DeletedByUserId;
            notification.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Notifications.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }

    public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public MarkNotificationReadCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var recipient = (await _unitOfWork.NotificationRecipients.GetAllAsync())
                .FirstOrDefault(r => r.NotificationId == request.NotificationId && r.UserId == request.UserId);
            if (recipient == null)
                return false;

            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null || !notification.IsActive)
                return false;

            if (recipient.IsRead)
                return true;

            recipient.IsRead = true;
            recipient.ReadDate = DateTime.UtcNow;
            await _unitOfWork.NotificationRecipients.UpdateAsync(recipient);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}

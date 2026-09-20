using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using MediatR;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetAdminNotificationsQueryHandler : IRequestHandler<GetAdminNotificationsQuery, IReadOnlyList<AdminNotificationListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAdminNotificationsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IReadOnlyList<AdminNotificationListItemDto>> Handle(
            GetAdminNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var notifications = (await _unitOfWork.Notifications.GetAllAsync())
                .Where(n => request.IncludeInactive || n.IsActive)
                .OrderByDescending(n => n.CreatedDate)
                .ToList();

            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var targets = (await _unitOfWork.NotificationTargets.GetAllAsync()).ToList();
            var recipients = (await _unitOfWork.NotificationRecipients.GetAllAsync()).ToList();

            return notifications.Select(n =>
            {
                var targetIds = targets.Where(t => t.NotificationId == n.NotificationId)
                    .Select(t => t.DealershipId)
                    .ToList();
                var names = targetIds.Select(id => dealerships.TryGetValue(id, out var d) ? d.DealershipName : id.ToString()).ToList();
                var rows = recipients.Where(r => r.NotificationId == n.NotificationId).ToList();

                users.TryGetValue(n.CreatedByUserId, out var creator);

                return new AdminNotificationListItemDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    LocationSummary = NotificationService.FormatLocationSummary(n.TargetAllLocations, names),
                    CreatedDate = n.CreatedDate,
                    CreatedByName = creator?.GetFullName() ?? $"User #{n.CreatedByUserId}",
                    RecipientCount = rows.Count,
                    ReadCount = rows.Count(r => r.IsRead),
                    IsActive = n.IsActive
                };
            }).ToList();
        }
    }

    public class GetAdminNotificationDetailQueryHandler : IRequestHandler<GetAdminNotificationDetailQuery, AdminNotificationDetailDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAdminNotificationDetailQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<AdminNotificationDetailDto?> Handle(
            GetAdminNotificationDetailQuery request,
            CancellationToken cancellationToken)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null)
                return null;

            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var subdealers = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(s => s.SubDealerId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var targetIds = (await _unitOfWork.NotificationTargets.GetAllAsync())
                .Where(t => t.NotificationId == notification.NotificationId)
                .Select(t => t.DealershipId)
                .ToList();
            var names = targetIds.Select(id => dealerships.TryGetValue(id, out var d) ? d.DealershipName : id.ToString()).ToList();
            var recipientRows = (await _unitOfWork.NotificationRecipients.GetAllAsync())
                .Where(r => r.NotificationId == notification.NotificationId)
                .OrderBy(r => r.DealershipId)
                .ThenBy(r => r.SubDealerId)
                .ThenBy(r => r.UserId)
                .ToList();

            users.TryGetValue(notification.CreatedByUserId, out var creator);
            string? modifiedByName = null;
            if (notification.ModifiedByUserId.HasValue
                && users.TryGetValue(notification.ModifiedByUserId.Value, out var modifier))
            {
                modifiedByName = modifier.GetFullName();
            }

            var recipients = recipientRows.Select(r =>
            {
                users.TryGetValue(r.UserId, out var user);
                subdealers.TryGetValue(r.SubDealerId, out var sub);
                dealerships.TryGetValue(r.DealershipId, out var dealer);
                return new NotificationRecipientStatusDto
                {
                    UserId = r.UserId,
                    UserName = user?.GetFullName() ?? user?.Username ?? $"User #{r.UserId}",
                    SubDealerName = sub?.SubDealerName ?? $"Org #{r.SubDealerId}",
                    DealershipName = dealer?.DealershipName ?? $"Location #{r.DealershipId}",
                    IsRead = r.IsRead,
                    ReadDate = r.ReadDate
                };
            }).ToList();

            return new AdminNotificationDetailDto
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Body = notification.Body,
                TargetAllLocations = notification.TargetAllLocations,
                DealershipIds = targetIds,
                LocationSummary = NotificationService.FormatLocationSummary(notification.TargetAllLocations, names),
                CreatedDate = notification.CreatedDate,
                CreatedByName = creator?.GetFullName() ?? $"User #{notification.CreatedByUserId}",
                ModifiedDate = notification.ModifiedDate,
                ModifiedByName = modifiedByName,
                IsActive = notification.IsActive,
                RecipientCount = recipients.Count,
                ReadCount = recipients.Count(r => r.IsRead),
                Recipients = recipients
            };
        }
    }

    public class GetSubdealerNotificationsQueryHandler : IRequestHandler<GetSubdealerNotificationsQuery, IReadOnlyList<SubdealerNotificationListItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealerNotificationsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IReadOnlyList<SubdealerNotificationListItemDto>> Handle(
            GetSubdealerNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var activeNotificationIds = (await _unitOfWork.Notifications.GetAllAsync())
                .Where(n => n.IsActive)
                .Select(n => n.NotificationId)
                .ToHashSet();

            var recipients = (await _unitOfWork.NotificationRecipients.GetAllAsync())
                .Where(r => r.UserId == request.UserId && activeNotificationIds.Contains(r.NotificationId))
                .ToList();

            var notifications = (await _unitOfWork.Notifications.GetAllAsync())
                .Where(n => activeNotificationIds.Contains(n.NotificationId))
                .ToDictionary(n => n.NotificationId);

            return recipients
                .Where(r => notifications.ContainsKey(r.NotificationId))
                .Select(r =>
                {
                    var notification = notifications[r.NotificationId];
                    return new SubdealerNotificationListItemDto
                    {
                        NotificationId = notification.NotificationId,
                        Title = notification.Title,
                        BodyPreview = Truncate(notification.Body, 120),
                        CreatedDate = notification.CreatedDate,
                        IsRead = r.IsRead,
                        ReadDate = r.ReadDate
                    };
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        private static string Truncate(string value, int max)
        {
            var text = value.Replace("\r\n", " ").Replace('\n', ' ').Trim();
            return text.Length <= max ? text : text[..max] + "…";
        }
    }

    public class GetSubdealerNotificationDetailQueryHandler : IRequestHandler<GetSubdealerNotificationDetailQuery, SubdealerNotificationDetailDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealerNotificationDetailQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<SubdealerNotificationDetailDto?> Handle(
            GetSubdealerNotificationDetailQuery request,
            CancellationToken cancellationToken)
        {
            var recipient = (await _unitOfWork.NotificationRecipients.GetAllAsync())
                .FirstOrDefault(r => r.NotificationId == request.NotificationId && r.UserId == request.UserId);
            if (recipient == null)
                return null;

            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null || !notification.IsActive)
                return null;

            return new SubdealerNotificationDetailDto
            {
                NotificationId = notification.NotificationId,
                Title = notification.Title,
                Body = notification.Body,
                CreatedDate = notification.CreatedDate,
                IsRead = recipient.IsRead,
                ReadDate = recipient.ReadDate
            };
        }
    }

    public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUnreadNotificationCountQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        {
            var activeNotificationIds = (await _unitOfWork.Notifications.GetAllAsync())
                .Where(n => n.IsActive)
                .Select(n => n.NotificationId)
                .ToHashSet();

            return (await _unitOfWork.NotificationRecipients.GetAllAsync())
                .Count(r => r.UserId == request.UserId
                    && !r.IsRead
                    && activeNotificationIds.Contains(r.NotificationId));
        }
    }

    public class PreviewNotificationRecipientCountQueryHandler : IRequestHandler<PreviewNotificationRecipientCountQuery, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public PreviewNotificationRecipientCountQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<int> Handle(PreviewNotificationRecipientCountQuery request, CancellationToken cancellationToken)
        {
            var dealershipIds = await NotificationService.ResolveTargetDealershipIdsAsync(
                _unitOfWork, request.TargetAllLocations, request.DealershipIds);
            var recipients = await NotificationService.ResolveSubdealerRecipientsAsync(_unitOfWork, dealershipIds);
            return recipients.Count;
        }
    }
}

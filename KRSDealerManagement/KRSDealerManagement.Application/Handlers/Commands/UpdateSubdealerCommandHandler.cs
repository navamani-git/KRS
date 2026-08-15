using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateSubdealerCommandHandler : IRequestHandler<UpdateSubdealerCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateSubdealerCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateSubdealerCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null || user.UserRole != 2)
                throw new InvalidOperationException("Subdealer not found.");

            var username = request.Username.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Username is required.");

            var duplicate = (await _unitOfWork.Users.GetAllAsync())
                .Any(u => u.UserId != request.UserId
                    && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                throw new InvalidOperationException("Username is already taken.");

            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId);
            if (dealership == null || !dealership.IsActive)
                throw new InvalidOperationException("Dealership location not found or inactive.");

            var assignment = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == request.UserId)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.IsPrimary)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Subdealer hierarchy assignment not found.");

            user.Username = username;
            user.Email = request.Email.Trim();
            user.FirstName = request.SubdealerName.Trim();
            user.LastName = request.Location.Trim();
            user.PhoneNumber = request.PrimaryPhone.Trim();
            user.IsActive = request.IsActive;
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);

            if (assignment.SubDealerId.HasValue)
            {
                var org = await _unitOfWork.SubDealers.GetByIdAsync(assignment.SubDealerId.Value);
                if (org != null)
                {
                    org.SubDealerCode = username;
                    org.SubDealerName = request.SubdealerName.Trim();
                    org.Location = request.Location.Trim();
                    org.Email = request.Email.Trim();
                    org.PrimaryPhone = request.PrimaryPhone.Trim();
                    org.SecondaryPhone = string.IsNullOrWhiteSpace(request.SecondaryPhone) ? null : request.SecondaryPhone.Trim();
                    org.SalesRepMobile = string.IsNullOrWhiteSpace(request.SalesRepMobile) ? null : request.SalesRepMobile.Trim();
                    org.ServiceRepMobile = string.IsNullOrWhiteSpace(request.ServiceRepMobile) ? null : request.ServiceRepMobile.Trim();
                    org.DealershipId = request.DealershipId;
                    org.IsActive = request.IsActive;
                    org.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.SubDealers.UpdateAsync(org);
                }
            }

            assignment.DealershipId = request.DealershipId;
            assignment.IsActive = request.IsActive;
            assignment.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.UserOrgRoles.UpdateAsync(assignment);

            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync())
                .Where(a => a.SubdealerId == request.UserId)
                .ToList();
            foreach (var account in accounts)
            {
                account.IsActive = request.IsActive;
                account.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.SubdealerAccounts.UpdateAsync(account);
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "SubDealer",
                entityId: assignment.SubDealerId ?? request.UserId,
                action: "Update",
                userId: request.UpdatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    request.UserId,
                    Username = username,
                    request.SubdealerName,
                    request.Location,
                    request.IsActive,
                    request.DealershipId
                }));

            return true;
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateSubdealerOrgCommandHandler : IRequestHandler<UpdateSubdealerOrgCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateSubdealerOrgCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateSubdealerOrgCommand request, CancellationToken cancellationToken)
        {
            var org = await _unitOfWork.SubDealers.GetByIdAsync(request.SubDealerId)
                ?? throw new InvalidOperationException("Subdealer not found.");

            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId);
            if (dealership == null || !dealership.IsActive)
                throw new InvalidOperationException("Dealership location not found or inactive.");

            if (await SubdealerOrgService.IsOrgNameTakenAsync(
                    _unitOfWork, request.DealershipId, request.SubdealerName, request.SubDealerId))
                throw new InvalidOperationException(
                    $"A subdealer named '{request.SubdealerName.Trim()}' already exists at this location.");

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

            var logins = await SubdealerOrgService.GetLoginsForOrgAsync(_unitOfWork, request.SubDealerId);
            foreach (var login in logins)
            {
                login.DealershipId = request.DealershipId;
                login.IsActive = request.IsActive;
                login.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.UserOrgRoles.UpdateAsync(login);

                var user = await _unitOfWork.Users.GetByIdAsync(login.UserId);
                if (user != null)
                {
                    user.IsActive = request.IsActive;
                    user.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.Users.UpdateAsync(user);

                    var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync())
                        .Where(a => a.SubdealerId == login.UserId);
                    foreach (var account in accounts)
                    {
                        account.IsActive = request.IsActive;
                        account.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.SubdealerAccounts.UpdateAsync(account);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "SubDealer",
                entityId: request.SubDealerId,
                action: "Update",
                userId: request.UpdatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    request.SubDealerId,
                    request.SubdealerName,
                    request.Location,
                    request.IsActive,
                    request.DealershipId
                }));

            return true;
        }
    }
}

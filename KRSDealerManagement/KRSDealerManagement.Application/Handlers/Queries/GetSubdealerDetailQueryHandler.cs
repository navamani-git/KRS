using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetSubdealerDetailQueryHandler : IRequestHandler<GetSubdealerDetailQuery, SubdealerDetailDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealerDetailQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<SubdealerDetailDto?> Handle(GetSubdealerDetailQuery request, CancellationToken cancellationToken)
        {
            int? orgId = request.SubDealerId;
            if (!orgId.HasValue && request.UserId.HasValue)
                orgId = await SubdealerOrgService.GetOrgIdForUserAsync(_unitOfWork, request.UserId.Value);

            if (!orgId.HasValue) return null;

            var org = await _unitOfWork.SubDealers.GetByIdAsync(orgId.Value);
            if (org == null) return null;

            if (request.DealershipId.HasValue && org.DealershipId != request.DealershipId)
                return null;

            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(org.DealershipId);
            var loginAssignments = await SubdealerOrgService.GetLoginsForOrgAsync(_unitOfWork, org.SubDealerId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToList();

            var logins = new List<SubdealerLoginDto>();
            foreach (var assignment in loginAssignments)
            {
                if (!users.TryGetValue(assignment.UserId, out var user)) continue;

                var permAccount = accounts
                    .Where(a => a.SubdealerId == user.UserId && a.IsActive)
                    .OrderByDescending(a => string.Equals(a.AccountType, "Login", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .FirstOrDefault();
                if (permAccount == null) continue;

                logins.Add(new SubdealerLoginDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    DisplayName = user.FirstName,
                    PasswordHash = user.PasswordHash,
                    IsPrimary = assignment.IsPrimary,
                    IsActive = user.IsActive && assignment.IsActive,
                    CanExport = user.CanExport,
                    PermissionAccountId = permAccount.AccountId,
                    CreatedDate = user.CreatedDate
                });
            }

            var primaryUserId = await SubdealerOrgService.GetPrimaryUserIdForOrgAsync(_unitOfWork, org.SubDealerId);
            int? walletAccountId = null;
            if (primaryUserId.HasValue)
            {
                var wallet = await SubdealerOrgService.GetWalletAccountAsync(_unitOfWork, primaryUserId.Value);
                walletAccountId = wallet?.AccountId;
            }

            return new SubdealerDetailDto
            {
                SubDealerId = org.SubDealerId,
                DealershipId = org.DealershipId,
                DealershipName = dealership?.DealershipName,
                SubdealerName = org.SubDealerName,
                Location = org.Location ?? "",
                Email = org.Email ?? "",
                PrimaryPhone = org.PrimaryPhone ?? "",
                SecondaryPhone = org.SecondaryPhone,
                SalesRepMobile = org.SalesRepMobile,
                ServiceRepMobile = org.ServiceRepMobile,
                IsActive = org.IsActive,
                CreatedDate = org.CreatedDate,
                PrimaryUserId = primaryUserId,
                WalletAccountId = walletAccountId,
                Logins = logins
            };
        }
    }
}

using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    /// <summary>
    /// Resolves subdealer org hierarchy: org → primary login → wallet / permission accounts.
    /// </summary>
    public static class SubdealerOrgService
    {
        public static async Task<UserOrgRole?> GetAssignmentAsync(IUnitOfWork unitOfWork, int userId)
        {
            return (await unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == userId && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .FirstOrDefault();
        }

        public static async Task<int?> GetOrgIdForUserAsync(IUnitOfWork unitOfWork, int userId)
            => (await GetAssignmentAsync(unitOfWork, userId))?.SubDealerId;

        /// <summary>All login user IDs for the subdealer org of <paramref name="loginUserId"/> (includes the user).</summary>
        public static async Task<HashSet<int>> GetOrgLoginUserIdsAsync(IUnitOfWork unitOfWork, int loginUserId)
        {
            var orgId = await GetOrgIdForUserAsync(unitOfWork, loginUserId);
            if (!orgId.HasValue)
                return new HashSet<int> { loginUserId };

            var ids = (await GetLoginsForOrgAsync(unitOfWork, orgId.Value))
                .Select(a => a.UserId)
                .ToHashSet();

            if (ids.Count == 0)
                ids.Add(loginUserId);

            return ids;
        }

        public static async Task<int?> GetPrimaryUserIdForOrgAsync(IUnitOfWork unitOfWork, int subDealerId)
        {
            var roles = await unitOfWork.Roles.GetAllAsync();
            var subRole = roles.FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
            if (subRole == null) return null;

            var assignment = (await unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.SubDealerId == subDealerId && a.RoleId == subRole.RoleId && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .ThenBy(a => a.UserId)
                .FirstOrDefault();

            return assignment?.UserId;
        }

        public static async Task<IReadOnlyList<UserOrgRole>> GetLoginsForOrgAsync(IUnitOfWork unitOfWork, int subDealerId)
        {
            var roles = await unitOfWork.Roles.GetAllAsync();
            var subRole = roles.FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
            if (subRole == null) return Array.Empty<UserOrgRole>();

            return (await unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.SubDealerId == subDealerId && a.RoleId == subRole.RoleId)
                .OrderByDescending(a => a.IsPrimary)
                .ThenBy(a => a.UserId)
                .ToList();
        }

        public static bool IsMainAccount(SubdealerAccount account)
            => string.Equals(account.AccountType, "Main", StringComparison.OrdinalIgnoreCase)
               || string.Equals(account.AccountName, "Main Account", StringComparison.OrdinalIgnoreCase);

        public static async Task<SubdealerAccount?> GetPermissionAccountAsync(IUnitOfWork unitOfWork, int userId)
        {
            var accounts = (await unitOfWork.SubdealerAccounts.GetAllAsync())
                .Where(a => a.SubdealerId == userId && a.IsActive)
                .ToList();

            return accounts.FirstOrDefault(a => string.Equals(a.AccountType, "Login", StringComparison.OrdinalIgnoreCase))
                   ?? accounts.FirstOrDefault(IsMainAccount)
                   ?? accounts.FirstOrDefault();
        }

        public static async Task<SubdealerAccount?> GetWalletAccountAsync(IUnitOfWork unitOfWork, int userId)
        {
            var orgId = await GetOrgIdForUserAsync(unitOfWork, userId);
            if (!orgId.HasValue)
            {
                var own = (await unitOfWork.SubdealerAccounts.GetAllAsync())
                    .Where(a => a.SubdealerId == userId && a.IsActive)
                    .ToList();
                return own.FirstOrDefault(IsMainAccount) ?? own.FirstOrDefault();
            }

            var primaryUserId = await GetPrimaryUserIdForOrgAsync(unitOfWork, orgId.Value);
            if (!primaryUserId.HasValue) return null;

            var primaryAccounts = (await unitOfWork.SubdealerAccounts.GetAllAsync())
                .Where(a => a.SubdealerId == primaryUserId.Value && a.IsActive)
                .ToList();

            return primaryAccounts.FirstOrDefault(IsMainAccount) ?? primaryAccounts.FirstOrDefault();
        }

        public static async Task<bool> IsOrgNameTakenAsync(IUnitOfWork unitOfWork, int dealershipId, string name, int? excludeOrgId = null)
        {
            var normalized = name.Trim();
            return (await unitOfWork.SubDealers.GetAllAsync())
                .Any(o => o.DealershipId == dealershipId
                    && o.SubDealerName.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                    && (!excludeOrgId.HasValue || o.SubDealerId != excludeOrgId.Value));
        }
    }

}

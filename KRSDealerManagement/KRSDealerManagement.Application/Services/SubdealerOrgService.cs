using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

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

        /// <summary>
        /// Normalizes a value that may be org SubDealerId or legacy login UserId to org SubDealerId.
        /// </summary>
        public static async Task<int> ResolveOrgIdAsync(IUnitOfWork unitOfWork, int subdealerIdOrLoginUserId)
        {
            if (subdealerIdOrLoginUserId <= 0)
                throw new ArgumentOutOfRangeException(nameof(subdealerIdOrLoginUserId));

            var org = await unitOfWork.SubDealers.GetByIdAsync(subdealerIdOrLoginUserId);
            if (org != null)
            {
                // Collision: login UserId can equal another org's SubDealerId (e.g. user 22 = THANGAM, org 22 = KPN).
                // When this id is an org key and its primary login is a different user, treat as org SubDealerId.
                var primaryUserId = await GetPrimaryUserIdForOrgAsync(unitOfWork, subdealerIdOrLoginUserId);
                if (primaryUserId.HasValue && primaryUserId.Value != subdealerIdOrLoginUserId)
                    return subdealerIdOrLoginUserId;

                var mapped = await GetOrgIdForUserAsync(unitOfWork, subdealerIdOrLoginUserId);
                if (mapped.HasValue)
                    return mapped.Value;

                return subdealerIdOrLoginUserId;
            }

            var mappedOnly = await GetOrgIdForUserAsync(unitOfWork, subdealerIdOrLoginUserId);
            if (mappedOnly.HasValue)
                return mappedOnly.Value;

            return subdealerIdOrLoginUserId;
        }

        /// <summary>Resolve org SubDealerId from a wallet row's login UserId (SubdealerAccounts.SubdealerId).</summary>
        public static async Task<int> ResolveOrgIdFromWalletUserIdAsync(IUnitOfWork unitOfWork, int walletUserId)
        {
            if (walletUserId <= 0)
                throw new ArgumentOutOfRangeException(nameof(walletUserId));

            var mapped = await GetOrgIdForUserAsync(unitOfWork, walletUserId);
            if (mapped.HasValue)
                return mapped.Value;

            var org = await unitOfWork.SubDealers.GetByIdAsync(walletUserId);
            if (org != null)
                return walletUserId;

            return walletUserId;
        }

        public static async Task<bool> IsSameOrgAsync(IUnitOfWork unitOfWork, int loginUserId, int targetSubdealerOrgOrUserId)
        {
            var loginOrgId = await ResolveOrgIdAsync(unitOfWork, loginUserId);
            var targetOrgId = await ResolveOrgIdAsync(unitOfWork, targetSubdealerOrgOrUserId);
            return loginOrgId == targetOrgId;
        }

        public static bool MatchesOrgId(int? storedSubdealerId, int orgId)
            => storedSubdealerId.HasValue && storedSubdealerId.Value == orgId;

        public static bool MatchesOrgId(int? storedSubdealerId, IReadOnlySet<int> orgIds)
            => storedSubdealerId.HasValue && orgIds.Contains(storedSubdealerId.Value);

        /// <summary>Display label when SubdealerId column stores SubDealers.SubDealerId.</summary>
        public static string ResolveOrgDisplayName(int? subDealerOrgId, IReadOnlyDictionary<int, SubDealer> orgs)
        {
            if (!subDealerOrgId.HasValue || subDealerOrgId.Value <= 0)
                return "Unknown";

            if (orgs.TryGetValue(subDealerOrgId.Value, out var org))
            {
                var location = string.IsNullOrWhiteSpace(org.Location) ? "" : $" ({org.Location})";
                return $"{org.SubDealerName}{location}";
            }

            return $"Subdealer #{subDealerOrgId}";
        }

        /// <summary>Backward-compatible: accepts org id or legacy login user id.</summary>
        public static string ResolveDisplayName(
            int? subdealerIdOrUserId,
            IReadOnlyList<UserOrgRole> userOrgRoles,
            IReadOnlyDictionary<int, SubDealer> orgs,
            IReadOnlyDictionary<int, User> users)
        {
            if (!subdealerIdOrUserId.HasValue || subdealerIdOrUserId.Value <= 0)
                return "Unknown";

            if (orgs.TryGetValue(subdealerIdOrUserId.Value, out var directOrg))
            {
                var location = string.IsNullOrWhiteSpace(directOrg.Location) ? "" : $" ({directOrg.Location})";
                return $"{directOrg.SubDealerName}{location}";
            }

            var assignment = userOrgRoles
                .Where(a => a.UserId == subdealerIdOrUserId.Value && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .FirstOrDefault();
            if (assignment?.SubDealerId is int orgId && orgs.TryGetValue(orgId, out var org))
            {
                var location = string.IsNullOrWhiteSpace(org.Location) ? "" : $" ({org.Location})";
                return $"{org.SubDealerName}{location}";
            }

            return users.TryGetValue(subdealerIdOrUserId.Value, out var user)
                ? user.GetFullName()
                : "Unknown";
        }

        /// <summary>All login user IDs for the subdealer org (accepts org SubDealerId or login UserId).</summary>
        public static async Task<HashSet<int>> GetOrgLoginUserIdsAsync(IUnitOfWork unitOfWork, int loginUserIdOrOrgId)
        {
            var directOrg = await unitOfWork.SubDealers.GetByIdAsync(loginUserIdOrOrgId);
            if (directOrg != null)
            {
                var directIds = (await GetLoginsForOrgAsync(unitOfWork, loginUserIdOrOrgId))
                    .Where(a => a.IsActive)
                    .Select(a => a.UserId)
                    .ToHashSet();
                return directIds.Count > 0 ? directIds : new HashSet<int>();
            }

            var orgId = await GetOrgIdForUserAsync(unitOfWork, loginUserIdOrOrgId);
            if (!orgId.HasValue)
                return new HashSet<int> { loginUserIdOrOrgId };

            var ids = (await GetLoginsForOrgAsync(unitOfWork, orgId.Value))
                .Select(a => a.UserId)
                .ToHashSet();

            if (ids.Count == 0)
                ids.Add(loginUserIdOrOrgId);

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

        public static async Task<SubdealerAccount?> GetOrgWalletAccountAsync(IUnitOfWork unitOfWork, int orgId)
        {
            var primaryUserId = await GetPrimaryUserIdForOrgAsync(unitOfWork, orgId);
            if (!primaryUserId.HasValue) return null;
            return await GetWalletAccountAsync(unitOfWork, primaryUserId.Value);
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

        public static async Task ValidateOwnShowroomToggleAsync(
            IUnitOfWork unitOfWork,
            int dealershipId,
            bool ownShowroom,
            int? excludeOrgId = null)
        {
            if (!ownShowroom) return;

            var existing = (await unitOfWork.SubDealers.GetAllAsync())
                .FirstOrDefault(o => o.DealershipId == dealershipId
                    && o.OwnShowroom
                    && (!excludeOrgId.HasValue || o.SubDealerId != excludeOrgId.Value));

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"This dealership already has an Own Showroom subdealer ({existing.SubDealerName}). Only one is allowed.");
            }
        }

        public static async Task<bool> HasActiveOwnShowroomAsync(IUnitOfWork unitOfWork, int dealershipId)
            => (await unitOfWork.SubDealers.GetAllAsync())
                .Any(o => o.DealershipId == dealershipId && o.OwnShowroom && o.IsActive);
    }

}

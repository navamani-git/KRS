using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    /// <summary>
    /// Resolves effective menu keys for a user (RoleMenus + subdealer AccountPermissions).
    /// </summary>
    public static class MenuAccessResolver
    {
        public static async Task<List<string>> ResolveAsync(IUnitOfWork unitOfWork, int userId, Role role)
        {
            var roleMenus = (await unitOfWork.RoleMenus.GetAllAsync())
                .Where(m => m.RoleId == role.RoleId && m.IsAccessible)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.MenuKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                return roleMenus;

            var accounts = (await unitOfWork.SubdealerAccounts.GetAllAsync())
                .Where(a => a.SubdealerId == userId && a.IsActive)
                .ToList();
            var account = accounts.FirstOrDefault();
            if (account == null)
                return roleMenus;

            var perms = (await unitOfWork.AccountPermissions.GetAllAsync())
                .Where(p => p.AccountId == account.AccountId)
                .ToList();

            if (!perms.Any())
                return roleMenus;

            // Subdealer: AccountPermissions are the source of truth when configured
            var allowed = perms
                .Where(p => p.IsAccessible)
                .Select(p => p.MenuKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sortOrder = MenuKeys.GetSubdealerConfigurableMenus()
                .Select((m, i) => (m.Key, i))
                .ToDictionary(x => x.Key, x => x.i, StringComparer.OrdinalIgnoreCase);

            return allowed
                .OrderBy(k => sortOrder.TryGetValue(k, out var i) ? i : 999)
                .ThenBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

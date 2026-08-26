using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Services
{
    /// <summary>
    /// Resolves effective menu access for a user (RoleMenus + subdealer AccountPermissions).
    /// </summary>
    public static class MenuAccessResolver
    {
        public static async Task<List<MenuAccessEntry>> ResolveEntriesAsync(IUnitOfWork unitOfWork, int userId, Role role)
        {
            if (role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return StaffMenuAccess.AllAdminMenus()
                    .Select((m, i) => new MenuAccessEntry { MenuKey = m.Key, Level = MenuAccessLevel.Full })
                    .ToList();
            }

            var roleMenus = (await unitOfWork.RoleMenus.GetAllAsync())
                .Where(m => m.RoleId == role.RoleId && m.IsAccessible)
                .OrderBy(m => m.SortOrder)
                .ToList();

            if (!role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
            {
                if (roleMenus.Count == 0)
                {
                    var legacyRole = RoleTemplateDefaults.MapTemplateToLegacyUserRole(role.RoleTemplateCode);
                    return StaffMenuAccess.GetMenusForRole(legacyRole)
                        .Select(k => new MenuAccessEntry { MenuKey = k, Level = MenuAccessLevel.Full })
                        .ToList();
                }

                return roleMenus
                    .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                    .Select(g =>
                    {
                        var row = g.First();
                        return new MenuAccessEntry
                        {
                            MenuKey = row.MenuKey,
                            Level = row.IsReadOnly ? MenuAccessLevel.ReadOnly : MenuAccessLevel.Full
                        };
                    })
                    .ToList();
            }

            var entries = roleMenus
                .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var row = g.First();
                    return new MenuAccessEntry
                    {
                        MenuKey = row.MenuKey,
                        Level = row.IsReadOnly ? MenuAccessLevel.ReadOnly : MenuAccessLevel.Full
                    };
                })
                .ToList();

            var account = await SubdealerOrgService.GetPermissionAccountAsync(unitOfWork, userId);
            if (account == null)
                return entries;

            var perms = (await unitOfWork.AccountPermissions.GetAllAsync())
                .Where(p => p.AccountId == account.AccountId)
                .ToList();

            if (!perms.Any())
                return entries;

            return perms
                .Where(p => p.IsAccessible)
                .Select(p => new MenuAccessEntry { MenuKey = p.MenuKey, Level = MenuAccessLevel.Full })
                .GroupBy(e => e.MenuKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(e => e.MenuKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static async Task<List<string>> ResolveAsync(IUnitOfWork unitOfWork, int userId, Role role)
        {
            var entries = await ResolveEntriesAsync(unitOfWork, userId, role);
            return entries
                .Where(e => e.Level != MenuAccessLevel.None)
                .Select(e => e.MenuKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static async Task<Dictionary<string, MenuAccessLevel>> ResolveMapAsync(IUnitOfWork unitOfWork, int userId, Role role)
        {
            var entries = await ResolveEntriesAsync(unitOfWork, userId, role);
            return entries
                .Where(e => e.Level != MenuAccessLevel.None)
                .GroupBy(e => e.MenuKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Level, StringComparer.OrdinalIgnoreCase);
        }
    }
}

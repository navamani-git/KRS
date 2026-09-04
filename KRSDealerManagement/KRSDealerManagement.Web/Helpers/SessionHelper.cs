using Microsoft.AspNetCore.Http;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Web.Helpers
{
    public static class SessionHelper
    {
        private const string SessionKeyUserId = "UserId";
        private const string SessionKeyUsername = "Username";
        private const string SessionKeyFullName = "FullName";
        private const string SessionKeyUserRole = "UserRole";
        private const string SessionKeyRoleName = "RoleName";
        private const string SessionKeyRoleCode = "RoleCode";
        private const string SessionKeyDealershipId = "DealershipId";
        private const string SessionKeyDealershipName = "DealershipName";
        private const string SessionKeySubDealerId = "SubDealerId";
        private const string SessionKeyMenus = "AccessibleMenus";
        private const string SessionKeyMenuAccess = "MenuAccessLevels";
        private const string SessionKeyCanExport = "CanExport";
        private const string SessionKeyQuickActionKeys = "QuickActionKeys";
        private const string SessionKeyDashboardWidgetKeys = "DashboardWidgetKeys";

        public static void SetUserSession(
            ISession session,
            int userId,
            string username,
            string fullName,
            int userRole,
            string roleName,
            string roleCode,
            int? dealershipId = null,
            string? dealershipName = null,
            int? subDealerId = null,
            IEnumerable<string>? menuKeys = null,
            IDictionary<string, MenuAccessLevel>? menuAccess = null,
            bool canExport = true,
            string? quickActionKeys = null,
            string? dashboardWidgetKeys = null)
        {
            session.SetInt32(SessionKeyUserId, userId);
            session.SetString(SessionKeyUsername, username);
            session.SetString(SessionKeyFullName, fullName);
            session.SetInt32(SessionKeyUserRole, userRole);
            session.SetString(SessionKeyRoleName, roleName);
            session.SetString(SessionKeyRoleCode, roleCode);
            if (dealershipId.HasValue) session.SetInt32(SessionKeyDealershipId, dealershipId.Value);
            else session.Remove(SessionKeyDealershipId);
            if (!string.IsNullOrWhiteSpace(dealershipName)) session.SetString(SessionKeyDealershipName, dealershipName);
            else session.Remove(SessionKeyDealershipName);
            if (subDealerId.HasValue) session.SetInt32(SessionKeySubDealerId, subDealerId.Value);
            else session.Remove(SessionKeySubDealerId);
            session.SetString(SessionKeyMenus, string.Join(",", menuKeys ?? Array.Empty<string>()));
            session.SetString(SessionKeyMenuAccess, SerializeMenuAccess(menuAccess));
            session.SetString(SessionKeyCanExport, canExport ? "1" : "0");
            if (quickActionKeys != null)
                session.SetString(SessionKeyQuickActionKeys, quickActionKeys);
            else
                session.Remove(SessionKeyQuickActionKeys);
            if (dashboardWidgetKeys != null)
                session.SetString(SessionKeyDashboardWidgetKeys, dashboardWidgetKeys);
            else
                session.Remove(SessionKeyDashboardWidgetKeys);
        }

        public static int? GetUserId(ISession session) => session.GetInt32(SessionKeyUserId);
        public static string? GetUsername(ISession session) => session.GetString(SessionKeyUsername);

        public static void UpdateUsername(ISession session, string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
                session.SetString(SessionKeyUsername, username.Trim().ToLowerInvariant());
        }

        public static string? GetFullName(ISession session) => session.GetString(SessionKeyFullName);
        public static int? GetUserRole(ISession session) => session.GetInt32(SessionKeyUserRole);
        public static string? GetRoleName(ISession session) => session.GetString(SessionKeyRoleName);
        public static string? GetRoleCode(ISession session) => session.GetString(SessionKeyRoleCode);
        public static int? GetDealershipId(ISession session) => session.GetInt32(SessionKeyDealershipId);
        public static string? GetDealershipName(ISession session) => session.GetString(SessionKeyDealershipName);
        public static int? GetSubDealerId(ISession session) => session.GetInt32(SessionKeySubDealerId);

        public static bool IsAuthenticated(ISession session) => session.GetInt32(SessionKeyUserId).HasValue;

        public static bool IsSystemAdmin(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || GetUserRole(session) == 1;

        public static bool IsAdmin(ISession session) => IsSystemAdmin(session);

        public static bool IsBranchManager(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.BranchManager, StringComparison.OrdinalIgnoreCase);

        public static bool IsFinanceAdmin(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.FinanceAdmin, StringComparison.OrdinalIgnoreCase)
            || GetUserRole(session) == 3;

        public static bool IsSubdealer(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase)
            || GetUserRole(session) == 2;

        public static bool IsStaff(ISession session) =>
            IsSystemAdmin(session) || (!IsSubdealer(session) && GetMenuAccessLevel(session, StaffMenuAccess.Balances) != MenuAccessLevel.None)
            || IsBranchManager(session) || IsFinanceAdmin(session)
            || HasAnyConfiguredStaffMenu(session);

        public static int? GetDealershipScope(ISession session)
        {
            if (IsSystemAdmin(session)) return null;
            return GetDealershipId(session);
        }

        public static MenuAccessLevel GetMenuAccessLevel(ISession session, string menuKey)
        {
            if (IsSystemAdmin(session)) return MenuAccessLevel.Full;

            var map = GetMenuAccessMap(session);
            if (map.TryGetValue(menuKey, out var level))
                return level;

            if (StaffMenuAccess.IsBookingMilestoneKey(menuKey)
                && map.TryGetValue(StaffMenuAccess.VehicleBookings, out var inheritedBooking))
            {
                return inheritedBooking;
            }

            if (IsSubdealer(session)
                && string.Equals(menuKey, MenuKeys.VehiclesBookingStages, StringComparison.OrdinalIgnoreCase)
                && map.TryGetValue(MenuKeys.VehiclesView, out var vehiclesView))
            {
                return vehiclesView;
            }

            return MenuAccessLevel.None;
        }

        public static bool HasMenuAccess(ISession session, string menuKey)
            => GetMenuAccessLevel(session, menuKey) != MenuAccessLevel.None;

        public static bool HasAnyBookingStaffMenuAccess(ISession session)
            => StaffMenuAccess.AllBookingStaffMenuKeys()
                .Any(key => HasMenuAccess(session, key));

        public static bool IsMenuReadOnly(ISession session, string menuKey)
            => GetMenuAccessLevel(session, menuKey) == MenuAccessLevel.ReadOnly;

        public static bool CanWriteMenu(ISession session, string menuKey)
            => GetMenuAccessLevel(session, menuKey) == MenuAccessLevel.Full;

        public static bool CanExportMenu(ISession session, string menuKey)
        {
            if (!CanExport(session)) return false;
            var level = GetMenuAccessLevel(session, menuKey);
            return level is MenuAccessLevel.ReadOnly or MenuAccessLevel.Full;
        }

        public static bool CanExport(ISession session)
        {
            if (IsSystemAdmin(session)) return true;
            var raw = session.GetString(SessionKeyCanExport);
            return raw != "0";
        }

        public static string? GetQuickActionKeys(ISession session) => session.GetString(SessionKeyQuickActionKeys);

        public static string? GetDashboardWidgetKeys(ISession session) => session.GetString(SessionKeyDashboardWidgetKeys);

        public static Dictionary<string, MenuAccessLevel> GetMenuAccessMap(ISession session)
        {
            var raw = session.GetString(SessionKeyMenuAccess);
            if (string.IsNullOrWhiteSpace(raw))
                return BuildLegacyMenuAccessMap(session);

            var map = new Dictionary<string, MenuAccessLevel>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var bits = part.Split(':', 2);
                if (bits.Length != 2) continue;
                if (Enum.TryParse<MenuAccessLevel>(bits[1], out var level) && level != MenuAccessLevel.None)
                    map[bits[0]] = level;
            }

            return map;
        }

        public static void ClearSession(ISession session) => session.Clear();

        private static bool HasAnyConfiguredStaffMenu(ISession session)
        {
            var raw = session.GetString(SessionKeyMenus);
            return !string.IsNullOrWhiteSpace(raw) && !IsSubdealer(session);
        }

        private static Dictionary<string, MenuAccessLevel> BuildLegacyMenuAccessMap(ISession session)
        {
            var map = new Dictionary<string, MenuAccessLevel>(StringComparer.OrdinalIgnoreCase);
            var raw = session.GetString(SessionKeyMenus);
            if (string.IsNullOrWhiteSpace(raw))
            {
                var role = GetUserRole(session);
                if (role.HasValue)
                {
                    foreach (var key in StaffMenuAccess.GetMenusForRole(role.Value))
                        map[key] = MenuAccessLevel.Full;
                }
                return map;
            }

            foreach (var key in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                map[key] = MenuAccessLevel.Full;
            return map;
        }

        private static string SerializeMenuAccess(IDictionary<string, MenuAccessLevel>? menuAccess)
        {
            if (menuAccess == null || menuAccess.Count == 0) return string.Empty;
            return string.Join(",", menuAccess
                .Where(kv => kv.Value != MenuAccessLevel.None)
                .Select(kv => $"{kv.Key}:{(int)kv.Value}"));
        }
    }
}

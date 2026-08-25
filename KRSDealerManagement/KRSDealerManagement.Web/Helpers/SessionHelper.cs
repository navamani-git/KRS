using Microsoft.AspNetCore.Http;
using KRSDealerManagement.Shared.Constants;

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
            IEnumerable<string>? menuKeys = null)
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
        }

        public static int? GetUserId(ISession session) => session.GetInt32(SessionKeyUserId);
        public static string? GetUsername(ISession session) => session.GetString(SessionKeyUsername);
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

        public static bool IsAdmin(ISession session) => IsSystemAdmin(session); // legacy name

        public static bool IsBranchManager(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.BranchManager, StringComparison.OrdinalIgnoreCase)
            || GetUserRole(session) == 4;

        public static bool IsFinanceAdmin(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.FinanceAdmin, StringComparison.OrdinalIgnoreCase)
            || GetUserRole(session) == 3;

        public static bool IsSubdealer(ISession session) =>
            string.Equals(GetRoleCode(session), RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase)
            || GetUserRole(session) == 2;

        public static bool IsStaff(ISession session) =>
            IsSystemAdmin(session) || IsBranchManager(session) || IsFinanceAdmin(session);

        /// <summary>Null = all dealerships (system admin). Otherwise scoped.</summary>
        public static int? GetDealershipScope(ISession session)
        {
            if (IsSystemAdmin(session)) return null;
            return GetDealershipId(session);
        }

        public static bool HasMenuAccess(ISession session, string menuKey)
        {
            if (IsSystemAdmin(session)) return true;

            var raw = session.GetString(SessionKeyMenus);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var hasInSession = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(k => k.Equals(menuKey, StringComparison.OrdinalIgnoreCase));
                if (hasInSession) return true;
            }

            // Role defaults from code (e.g. Balances for branch manager when DB RoleMenus lags behind)
            var role = GetUserRole(session);
            return role.HasValue && StaffMenuAccess.CanAccess(role.Value, menuKey);
        }

        public static void ClearSession(ISession session) => session.Clear();
    }
}

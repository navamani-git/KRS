namespace KRSDealerManagement.Shared.Constants
{
    /// <summary>
    /// Stable role codes matching dbo.Roles.RoleCode.
    /// Prefer loading roles/menus from DB; these constants are for comparisons only.
    /// </summary>
    public static class RoleCodes
    {
        public const string SystemAdmin = "SYSTEM_ADMIN";
        public const string BranchManager = "BRANCH_MANAGER";
        public const string FinanceAdmin = "FINANCE_ADMIN";
        public const string Subdealer = "SUBDEALER";
    }
}

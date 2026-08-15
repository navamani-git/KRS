namespace KRSDealerManagement.Shared.Enums
{
    /// <summary>
    /// System-level user roles
    /// </summary>
    public enum UserRoleEnum
    {
        /// <summary>KRS system owner — full access across all dealers</summary>
        Admin = 1,

        /// <summary>Subdealer under a specific Dealer</summary>
        Subdealer = 2,

        /// <summary>Finance staff — balances, payments, reports (all dealers)</summary>
        FinanceAdmin = 3,

        /// <summary>Dealer branch manager — only their dealer + its subdealers</summary>
        DealerBranchManager = 4
    }

    public static class UserRoleEnumExtensions
    {
        public static string GetDisplayName(this UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.Admin => "System Admin",
                UserRoleEnum.Subdealer => "Subdealer",
                UserRoleEnum.FinanceAdmin => "Finance Admin",
                UserRoleEnum.DealerBranchManager => "Dealer Branch Manager",
                _ => "Unknown"
            };
        }

        public static bool IsSystemAdmin(this UserRoleEnum role) => role == UserRoleEnum.Admin;
        public static bool IsSubdealer(this UserRoleEnum role) => role == UserRoleEnum.Subdealer;
        public static bool IsFinanceAdmin(this UserRoleEnum role) => role == UserRoleEnum.FinanceAdmin;
        public static bool IsDealerBranchManager(this UserRoleEnum role) => role == UserRoleEnum.DealerBranchManager;
        public static bool IsStaff(this UserRoleEnum role) =>
            role is UserRoleEnum.Admin or UserRoleEnum.FinanceAdmin or UserRoleEnum.DealerBranchManager;
    }
}

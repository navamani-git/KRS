namespace KRSDealerManagement.Shared.Constants
{
    public static class RoleTemplateCodes
    {
        public const string System = "SYSTEM";
        public const string Subdealer = "SUBDEALER";
        public const string Manager = "MANAGER";
        public const string FinanceManager = "FINANCE_MANAGER";
        public const string InsuranceRtoManager = "INSURANCE_RTO_MANAGER";
        public const string WarrantyManager = "WARRANTY_MANAGER";
        public const string WarrantyStaff = "WARRANTY_STAFF";
        public const string Custom = "CUSTOM";

        public static IReadOnlyList<(string Code, string Name, string Description)> All => new List<(string, string, string)>
        {
            (Manager, "Branch / Operations Manager", "Subdealers, orders, vehicles, bookings, returns"),
            (FinanceManager, "Finance Manager", "Balances, payments, reports, account corrections"),
            (InsuranceRtoManager, "Insurance / RTO Manager", "Vehicle booking pipeline, insurance & RTO stages"),
            (WarrantyManager, "Warranty Manager", "Warranty claims and parts master (location-scoped)"),
            (WarrantyStaff, "Warranty Staff", "Warranty claims processing (location-scoped)"),
            (Custom, "Custom", "Pick menus manually")
        };
    }
}

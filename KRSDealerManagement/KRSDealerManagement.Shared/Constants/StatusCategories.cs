namespace KRSDealerManagement.Shared.Constants

{

    public static class StatusCategories

    {

        public const string Payment = "PAYMENT";

        public const string Commission = "COMMISSION";

        public const string Vehicle = "VEHICLE";
        public const string Warranty = "WARRANTY";



        // Legacy — kept for migration references only

        public const string Order = "ORDER";

        public const string Return = "RETURN";

        public const string OrderItem = "ORDER_ITEM";

        public const string Booking = "BOOKING";



        public static IReadOnlyList<(string Code, string Name)> All => new List<(string, string)>

        {

            (Vehicle, "Vehicle Lifecycle"),
            (Payment, "Payments"),
            (Commission, "Commissions"),
            (Warranty, "Warranty Claims")

        };



        public static string GetDisplayName(string? code)

        {

            if (string.IsNullOrWhiteSpace(code)) return "All Categories";

            return All.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)).Name ?? code;

        }



        public static bool IsValid(string? code)

            => !string.IsNullOrWhiteSpace(code)

               && All.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    }

}


namespace KRSDealerManagement.Shared.Helpers
{
    /// <summary>
    /// Short plain-language hints for dashboard count widgets (pending items).
    /// </summary>
    public static class DashboardWidgetHelp
    {
        public const string PendingOrders =
            "Purchase orders submitted and waiting for dealer approval";

        public const string PendingReturns =
            "Return requests waiting for dealer review or action";

        public const string PendingPayments =
            "Payment proofs uploaded and waiting for verification";

        public const string PendingCommissions =
            "Commission claims waiting for approval";

        public const string ShowroomStock =
            "Allocated to subdealers, not booked and not invoiced yet";

        public const string DealerStock =
            "Unallocated OEM inventory at the dealer (available for PO allocation)";

        public const string StockSection =
            "Current vehicle inventory. Click a box to open the full stock list.";

        public const string RtoSubsidyProgressSection =
            "Subsidy document uploads and registered vehicles waiting for number plate handover.";

        public const string ManageVehiclesSection =
            "Track where each booked vehicle is in the process. The number is how many vehicles are at that step right now. Click a box to open the full list.";
    }
}

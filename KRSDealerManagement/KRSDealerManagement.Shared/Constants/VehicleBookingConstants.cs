namespace KRSDealerManagement.Shared.Constants
{
    /// <summary>
    /// Booking workflow steps — values match UnifiedVehicleStatus (7–14).
    /// </summary>
    public static class VehicleBookingStatus
    {
        public const int Booked = UnifiedVehicleStatus.BookedToCustomer;
        public const int PapReceived = UnifiedVehicleStatus.PaperReceived;
        public const int Invoiced = UnifiedVehicleStatus.Invoiced;
        public const int Insured = UnifiedVehicleStatus.InsuranceCreated;
        public const int RtoRequested = UnifiedVehicleStatus.RtoRequested;
        public const int Registered = UnifiedVehicleStatus.Registered;
        public const int SubsidyApplied = UnifiedVehicleStatus.SubsidyIdCreated;
        public const int Delivered = UnifiedVehicleStatus.Delivered;

        public static bool IsStaffAssignable(int status) => UnifiedVehicleStatus.IsStaffAssignable(status);

        public static IReadOnlyList<(int Value, string Label)> All => new List<(int, string)>
        {
            (Booked, "Booked to Customer"),
            (PapReceived, "Paper Received"),
            (Invoiced, "Invoiced"),
            (Insured, "Insurance Created"),
            (RtoRequested, "RTO Requested"),
            (Registered, "Registered"),
            (SubsidyApplied, "Subsidy ID Created"),
            (Delivered, "Delivered")
        };
    }

    public static class VehiclePaymentModes
    {
        public const string Emi = "EMI";
        public const string NoHypothecation = "NO_HYP";
        public const string Cash = "CASH";

        public static IReadOnlyList<(string Value, string Label)> All => new List<(string, string)>
        {
            (Emi, "EMI"),
            (NoHypothecation, "Don't Mention Hypothecation In Invoice & RC"),
            (Cash, "Cash")
        };

        public static string GetLabel(string? value) =>
            All.FirstOrDefault(x => x.Value == value).Label ?? value ?? "-";
    }
}

namespace KRSDealerManagement.Shared.Helpers
{
    public static class TransactionReasonHelper
    {
        public static string Commission(string? chassisNumber)
            => $"Commission - {FormatChassis(chassisNumber)}";

        public static string Return(string? chassisNumber)
            => $"Return - {FormatChassis(chassisNumber)}";

        public static string Reassignment(string? chassisNumber)
            => $"Vehicle reassignment - {FormatChassis(chassisNumber)}";

        public static string ShowroomAllocation(string? chassisNumber)
            => $"Showroom allocation - {FormatChassis(chassisNumber)}";

        public static string FormatChassis(string? chassisNumber)
            => string.IsNullOrWhiteSpace(chassisNumber) ? "-" : chassisNumber.Trim().ToUpperInvariant();
    }
}

namespace KRSDealerManagement.Shared.Helpers
{
    public static class TransactionReasonHelper
    {
        public static string Commission(string? chassisNumber, string? modelName = null)
            => $"Commission\n{FormatChassis(chassisNumber)}\n{(modelName ?? "-").Trim()}";

        public static string Return(string? chassisNumber)
            => Return(chassisNumber, null, null);

        public static string Return(string? chassisNumber, string? modelName, string? colorName)
            => $"Return\n{FormatChassis(chassisNumber)}\n{(modelName ?? "-").Trim()}\n{(colorName ?? "-").Trim()}";

        public static string Reassignment(string? chassisNumber)
            => $"Vehicle reassignment - {FormatChassis(chassisNumber)}";

        public static string ShowroomAllocation(string? chassisNumber)
            => $"Showroom allocation - {FormatChassis(chassisNumber)}";

        public static string FormatChassis(string? chassisNumber)
            => string.IsNullOrWhiteSpace(chassisNumber) ? "-" : chassisNumber.Trim().ToUpperInvariant();
    }
}

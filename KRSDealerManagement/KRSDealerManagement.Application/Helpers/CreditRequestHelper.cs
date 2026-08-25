namespace KRSDealerManagement.Application.Helpers
{
    public static class CreditRequestHelper
    {
        public const string TypeCode = "CREDIT_REQUEST";

        public static bool IsCreditRequestType(string? typeCode)
            => string.Equals(typeCode, TypeCode, StringComparison.OrdinalIgnoreCase);

        public static string FormatStatementReason(
            int paymentId,
            string? chassis,
            string? modelName,
            string? colorName)
        {
            var detail = new List<string>();
            if (!string.IsNullOrWhiteSpace(chassis))
                detail.Add(chassis.Trim());
            var modelColor = string.Join(" — ",
                new[] { modelName?.Trim(), colorName?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(modelColor))
                detail.Add(modelColor);

            return detail.Count == 0
                ? $"Credit Request #{paymentId}"
                : $"Credit Request #{paymentId} — {string.Join(" / ", detail)}";
        }
    }
}

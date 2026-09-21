using System.Text.RegularExpressions;
using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class PaymentStatementDescriptionHelper
    {
        private static readonly Regex PaymentIdPrefixPattern = new(
            @"^(Payment|Credit Request)\s*#\d+\s*(approved\s*[—-]\s*)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string FormatApprovalDescription(Payment payment, string? financeName)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(payment.CustomerName))
                parts.Add($"Customer: {payment.CustomerName.Trim()}");
            if (!string.IsNullOrWhiteSpace(payment.VinNumber))
                parts.Add($"VIN: {payment.VinNumber.Trim()}");
            if (!string.IsNullOrWhiteSpace(payment.PaymentType))
                parts.Add(payment.PaymentType.Trim());
            if (!string.IsNullOrWhiteSpace(financeName))
                parts.Add($"Finance: {financeName.Trim()}");

            if (!string.IsNullOrWhiteSpace(payment.CreditRequestModelName))
            {
                var modelColor = string.Join(" — ",
                    new[] { payment.CreditRequestModelName?.Trim(), payment.CreditRequestColorName?.Trim() }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(modelColor))
                    parts.Add(modelColor);
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : "Payment approved";
        }

        public static string NormalizeStoredReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return "";

            var trimmed = reason.Trim();
            var stripped = PaymentIdPrefixPattern.Replace(trimmed, "").Trim();
            return string.IsNullOrWhiteSpace(stripped) ? trimmed : stripped;
        }
    }
}

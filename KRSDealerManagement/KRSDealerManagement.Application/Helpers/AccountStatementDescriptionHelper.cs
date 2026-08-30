using System.Text.RegularExpressions;

namespace KRSDealerManagement.Application.Helpers
{
    /// <summary>
    /// Formats and normalizes account statement description text for vehicle-related debits.
    /// </summary>
    public static class AccountStatementDescriptionHelper
    {
        private static readonly Regex OrderLinePattern = new(
            @"^(ORD[-\s]?|PO[-\s#]?|ORDER[-\s#]?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string FormatVehicle(string chassis, string modelName, string colorName)
            => OrderTransactionReasonHelper.Format(chassis, modelName, colorName);

        /// <summary>
        /// Strips legacy order-number lines and expands model/color to separate lines for display.
        /// </summary>
        public static string NormalizeOrderVehicleReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return "";

            var lines = reason
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (lines.Count == 0)
                return reason.Trim();

            if (lines.Count >= 3 && LooksLikeOrderLine(lines[0]))
                lines.RemoveAt(0);

            if (lines.Count == 2 && lines[1].Contains('—'))
            {
                var modelColor = lines[1].Split('—', 2, StringSplitOptions.TrimEntries);
                if (modelColor.Length == 2)
                    return $"{lines[0]}\n{modelColor[0]}\n{modelColor[1]}";
            }

            return string.Join("\n", lines);
        }

        private static bool LooksLikeOrderLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.StartsWith("Order ", StringComparison.OrdinalIgnoreCase))
                return true;

            return OrderLinePattern.IsMatch(line.Trim());
        }
    }
}

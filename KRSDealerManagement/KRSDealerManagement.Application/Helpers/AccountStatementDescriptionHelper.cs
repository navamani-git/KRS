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

        private static readonly Regex SingleLineWithChassisPattern = new(
            @"^(?<body>.+?)\s*\((?<chassis>[A-Z0-9][A-Z0-9\-]{5,})\)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string FormatVehicle(string chassis, string modelName, string colorName)
            => OrderTransactionReasonHelper.Format(modelName, colorName, chassis);

        /// <summary>
        /// Strips legacy order-number lines and normalizes vehicle text to model / color / chassis lines.
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

            var prefix = "";
            if (lines.Count > 0 && IsCategoryPrefix(lines[0]))
            {
                prefix = lines[0];
                lines.RemoveAt(0);
            }

            if (lines.Count >= 3 && LooksLikeOrderLine(lines[0]))
                lines.RemoveAt(0);

            lines = NormalizeVehicleLines(lines);

            if (!string.IsNullOrEmpty(prefix))
                lines.Insert(0, prefix);

            return string.Join("\n", lines);
        }

        private static List<string> NormalizeVehicleLines(List<string> lines)
        {
            if (lines.Count == 0)
                return lines;

            if (lines.Count == 3 && LooksLikeChassisLine(lines[0]) && !LooksLikeChassisLine(lines[1]))
                return new List<string> { lines[1], lines[2], lines[0] };

            if (lines.Count == 2 && lines[1].Contains('—'))
            {
                var modelColor = lines[1].Split('—', 2, StringSplitOptions.TrimEntries);
                if (modelColor.Length == 2)
                    return new List<string> { modelColor[0], modelColor[1], lines[0] };
            }

            if (lines.Count == 1)
            {
                var fromSingle = TryParseSingleLineVehicle(lines[0]);
                if (fromSingle != null)
                    return fromSingle;
            }

            return lines;
        }

        private static List<string>? TryParseSingleLineVehicle(string line)
        {
            var match = SingleLineWithChassisPattern.Match(line.Trim());
            if (!match.Success)
                return null;

            var chassis = match.Groups["chassis"].Value.Trim().ToUpperInvariant();
            var body = match.Groups["body"].Value.Trim();
            var lastSpace = body.LastIndexOf(' ');
            if (lastSpace <= 0)
                return new List<string> { body, "-", chassis };

            var model = body[..lastSpace].Trim();
            var color = body[(lastSpace + 1)..].Trim();
            return new List<string> { model, color, chassis };
        }

        private static bool IsCategoryPrefix(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            return line.StartsWith("Return", StringComparison.OrdinalIgnoreCase)
                   || line.StartsWith("Commission", StringComparison.OrdinalIgnoreCase)
                   || line.StartsWith("Showroom allocation", StringComparison.OrdinalIgnoreCase)
                   || line.StartsWith("Vehicle reassignment", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeOrderLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.StartsWith("Order ", StringComparison.OrdinalIgnoreCase))
                return true;

            return OrderLinePattern.IsMatch(line.Trim());
        }

        private static bool LooksLikeChassisLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            if (trimmed.Length < 6)
                return false;

            if (trimmed.Contains(' '))
                return false;

            return trimmed.All(c => char.IsLetterOrDigit(c) || c == '-');
        }
    }
}

namespace KRSDealerManagement.Application.Helpers
{
    public static class CorrectionNoteHelper
    {
        public const int DefaultMaxRemarksLength = 450;

        public static string Append(string? existing, string entry, int maxLength = DefaultMaxRemarksLength)
        {
            if (string.IsNullOrWhiteSpace(existing)) return TrimToMax(entry, maxLength);
            var combined = $"{existing.TrimEnd()}\n{entry}";
            return TrimToMax(combined, maxLength);
        }

        private static string TrimToMax(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            var marker = "... [earlier notes truncated] ...\n";
            if (maxLength <= marker.Length + 20)
                return value[^maxLength..];

            var keep = maxLength - marker.Length;
            return marker + value[^keep..];
        }

        public static string FormatEntry(string correctedByName, string reason, IEnumerable<string> changes)
        {
            var changeLines = changes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

            var header = $"{DateTime.UtcNow:dd MMM yyyy, h:mm tt} UTC — {correctedByName.Trim()} updated this record.";
            var reasonLine = $"Reason: {reason.Trim()}";

            if (changeLines.Count == 0)
                return $"{header}\n{reasonLine}\n• No field changes recorded.";

            var bullets = string.Join("\n", changeLines.Select(c => $"• {c}"));
            return $"{header}\n{reasonLine}\n{bullets}";
        }

        public static string DescribeChange(string field, object? oldValue, object? newValue)
        {
            return $"{field}: {Display(oldValue)} → {Display(newValue)}";
        }

        private static string Display(object? value)
        {
            if (value == null)
                return "Not set";

            if (value is bool b)
                return CorrectionNoteLabelResolver.YesNo(b);

            var text = value.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? "Not set" : text;
        }
    }
}

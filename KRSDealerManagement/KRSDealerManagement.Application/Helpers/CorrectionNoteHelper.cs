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
            var changeText = changes.Any()
                ? string.Join("; ", changes)
                : "No field changes recorded";
            return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Correction by {correctedByName}: {reason.Trim()}. Changes: {changeText}.";
        }

        public static string DescribeChange(string field, object? oldValue, object? newValue)
        {
            return $"{field}: '{oldValue ?? "-"}' → '{newValue ?? "-"}'";
        }
    }
}

namespace KRSDealerManagement.Application.Helpers
{
    public static class CorrectionNoteHelper
    {
        public static string Append(string? existing, string entry)
        {
            if (string.IsNullOrWhiteSpace(existing)) return entry;
            return $"{existing.TrimEnd()} {entry}";
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

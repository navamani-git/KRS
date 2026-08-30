namespace KRSDealerManagement.Application.Helpers
{
    /// <summary>
    /// Server-side column filter helpers for grid screens.
    /// </summary>
    public static class GridFilterHelper
    {
        public static bool MatchesContains(string? value, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            return value != null
                && value.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesContainsAny(string? filter, params string?[] values)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            return values.Any(v => MatchesContains(v, filter));
        }

        public static bool MatchesDate(DateTime? value, DateTime? from, DateTime? to)
        {
            if (!from.HasValue && !to.HasValue) return true;
            if (!value.HasValue) return false;
            var d = value.Value.Date;
            if (from.HasValue && d < from.Value.Date) return false;
            if (to.HasValue && d > to.Value.Date) return false;
            return true;
        }

        public static bool MatchesDateTime(DateTime value, DateTime? from, DateTime? to)
            => MatchesDate(value, from, to);

        public static bool MatchesExact(string? value, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            return string.Equals(value?.Trim(), filter.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static string? GetFilter(IReadOnlyDictionary<string, string>? filters, string key)
        {
            if (filters == null || filters.Count == 0) return null;
            return filters.TryGetValue(key, out var v) ? v : null;
        }

        public static DateTime? GetDateFilter(IReadOnlyDictionary<string, string>? filters, string key)
        {
            var raw = GetFilter(filters, key);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return DateTime.TryParse(raw, out var d) ? d.Date : null;
        }
    }
}

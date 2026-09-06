using System.Globalization;

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

        public const string SortColumnKey = "_gs";
        public const string SortDirKey = "_gd";

        public static bool TryGetSort(IReadOnlyDictionary<string, string>? filters, out string column, out bool descending)
        {
            column = "";
            descending = false;
            if (filters == null || filters.Count == 0)
                return false;
            if (!filters.TryGetValue(SortColumnKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                return false;
            if (raw.StartsWith('_'))
                return false;
            column = raw.Trim();
            descending = filters.TryGetValue(SortDirKey, out var dir)
                && dir.Equals("desc", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        public static IEnumerable<T> ApplySort<T>(
            IEnumerable<T> rows,
            IReadOnlyDictionary<string, string>? filters,
            IReadOnlyDictionary<string, Func<T, string?>> columns,
            IReadOnlyDictionary<string, Func<T, DateTime?>>? dateColumns = null)
        {
            if (!TryGetSort(filters, out var column, out var descending))
                return rows;

            var list = rows as IList<T> ?? rows.ToList();
            if (list.Count <= 1)
                return list;

            if (dateColumns != null && dateColumns.TryGetValue(column, out var dateSelector))
            {
                var dated = list.Select((item, index) => (item, index, value: dateSelector(item))).ToList();
                dated.Sort((a, b) =>
                {
                    if (a.value.HasValue != b.value.HasValue)
                        return a.value.HasValue ? -1 : 1;
                    if (!a.value.HasValue)
                        return a.index.CompareTo(b.index);
                    var cmp = a.value.Value.CompareTo(b.value.Value);
                    if (cmp == 0) return a.index.CompareTo(b.index);
                    return descending ? -cmp : cmp;
                });
                return dated.Select(x => x.item).ToList();
            }

            if (!columns.TryGetValue(column, out var textSelector))
                return list;

            var keyed = list.Select((item, index) => (item, index, key: textSelector(item))).ToList();
            keyed.Sort((a, b) =>
            {
                var cmp = CompareSortValues(a.key, b.key);
                if (cmp == 0) return a.index.CompareTo(b.index);
                return descending ? -cmp : cmp;
            });
            return keyed.Select(x => x.item).ToList();
        }

        private static int CompareSortValues(string? left, string? right)
        {
            var emptyLeft = string.IsNullOrWhiteSpace(left);
            var emptyRight = string.IsNullOrWhiteSpace(right);
            if (emptyLeft && emptyRight) return 0;
            if (emptyLeft) return 1;
            if (emptyRight) return -1;

            var a = left!.Trim();
            var b = right!.Trim();
            if (TryParseDecimal(a, out var na) && TryParseDecimal(b, out var nb))
                return na.CompareTo(nb);
            if (DateTime.TryParse(a, out var da) && DateTime.TryParse(b, out var db))
                return da.CompareTo(db);
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseDecimal(string value, out decimal number)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                return true;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-IN"), out number);
        }
    }
}

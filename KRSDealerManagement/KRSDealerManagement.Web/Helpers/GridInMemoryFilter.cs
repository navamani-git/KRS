using KRSDealerManagement.Application.Helpers;

namespace KRSDealerManagement.Web.Helpers
{
    public static class GridInMemoryFilter
    {
        public static IEnumerable<T> ApplyTextFilters<T>(
            IEnumerable<T> source,
            IReadOnlyDictionary<string, string>? filters,
            IReadOnlyDictionary<string, Func<T, string?>> columns)
        {
            if (filters == null || filters.Count == 0) return source;

            return source.Where(item =>
            {
                foreach (var col in columns)
                {
                    var filter = GridFilterHelper.GetFilter(filters, col.Key);
                    if (string.IsNullOrWhiteSpace(filter)) continue;
                    if (!GridFilterHelper.MatchesContains(col.Value(item), filter))
                        return false;
                }
                return true;
            });
        }

        public static IEnumerable<T> ApplyDateFilters<T>(
            IEnumerable<T> source,
            IReadOnlyDictionary<string, string>? filters,
            IReadOnlyDictionary<string, Func<T, DateTime?>> columns)
        {
            if (filters == null || filters.Count == 0) return source;

            return source.Where(item =>
            {
                foreach (var col in columns)
                {
                    var dateFilter = GridFilterHelper.GetDateFilter(filters, col.Key);
                    if (!dateFilter.HasValue) continue;
                    if (!GridFilterHelper.MatchesDate(col.Value(item), dateFilter, dateFilter))
                        return false;
                }
                return true;
            });
        }
    }
}

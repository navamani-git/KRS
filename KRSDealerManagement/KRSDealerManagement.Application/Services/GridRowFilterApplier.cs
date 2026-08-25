using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    public static class GridRowFilterApplier
    {
        public static IEnumerable<T> Apply<T>(
            string gridId,
            IEnumerable<T> rows,
            IReadOnlyDictionary<string, string>? filters,
            IReadOnlyDictionary<string, Func<T, string?>> columns,
            IReadOnlyDictionary<string, Func<T, DateTime?>>? dateColumns = null)
        {
            if (filters == null || filters.Count == 0) return rows;

            return rows.Where(item =>
            {
                foreach (var col in columns)
                {
                    var filter = GridFilterHelper.GetFilter(filters, col.Key);
                    if (string.IsNullOrWhiteSpace(filter)) continue;
                    if (!GridFilterHelper.MatchesContains(col.Value(item), filter))
                        return false;
                }

                if (dateColumns != null)
                {
                    foreach (var col in dateColumns)
                    {
                        var dateFilter = GridFilterHelper.GetDateFilter(filters, col.Key);
                        if (!dateFilter.HasValue) continue;
                        if (!GridFilterHelper.MatchesDate(col.Value(item), dateFilter, dateFilter))
                            return false;
                    }
                }

                return true;
            });
        }
    }
}

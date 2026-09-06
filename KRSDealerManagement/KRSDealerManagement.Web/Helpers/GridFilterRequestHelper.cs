using KRSDealerManagement.Application.Helpers;
using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    public static class GridFilterRequestHelper
    {
        public const string Prefix = "cf_";

        public static Dictionary<string, string> ReadFilters(HttpRequest request)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in request.Query)
            {
                if (!kv.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var val = kv.Value.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                    dict[kv.Key[Prefix.Length..]] = val.Trim();
            }

            var sort = request.Query["sort"].ToString();
            if (!string.IsNullOrWhiteSpace(sort))
                dict[GridFilterHelper.SortColumnKey] = sort.Trim();
            var dir = request.Query["dir"].ToString();
            if (!string.IsNullOrWhiteSpace(dir))
                dict[GridFilterHelper.SortDirKey] = dir.Trim();

            return dict;
        }

        public static void ApplyToViewBag(dynamic viewBag, IReadOnlyDictionary<string, string> filters)
        {
            viewBag.ColumnFilters = filters;
        }

        public static string? Get(IReadOnlyDictionary<string, string>? filters, string key)
            => filters != null && filters.TryGetValue(key, out var v) ? v : null;
    }
}

using Microsoft.AspNetCore.Http;

namespace KRSDealerManagement.Web.Helpers
{
    public sealed record DashboardQuickAction(string Key, string Label, string Url, string Icon, string IconBg, string Group);

    public static class DashboardQuickActions
    {
        private const int DefaultVisibleCount = 8;
        public const string DashboardKey = "dashboard";

        private static readonly string[] IconBgs =
        {
            "bg-primary", "bg-success", "bg-info", "bg-warning", "bg-secondary", "bg-danger", "bg-teal"
        };

        public delegate string UrlBuilder(string controller, string action, IReadOnlyDictionary<string, object>? routeValues);

        /// <summary>All sidebar menu entries the user can access (same rules as the sidebar).</summary>
        public static IReadOnlyList<DashboardQuickAction> GetCatalog(ISession session, UrlBuilder urlBuilder)
        {
            var items = new List<DashboardQuickAction>();
            var iconIndex = 0;

            items.Add(new DashboardQuickAction(
                DashboardKey,
                "Dashboard",
                urlBuilder("Dashboard", "Index", null),
                "bi-speedometer2",
                IconBgs[iconIndex++ % IconBgs.Length],
                "Home"));

            foreach (var (groupName, item) in NavMenuHelper.EnumerateVisibleMenuItems(session))
                items.Add(ToQuickAction(item, groupName, urlBuilder, ref iconIndex));

            return items
                .GroupBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(i => i.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<DashboardQuickAction> Resolve(
            IReadOnlyList<DashboardQuickAction> catalog,
            string? savedKeys)
        {
            if (catalog.Count == 0) return catalog;

            // null = never customized → sensible defaults
            if (savedKeys == null)
                return catalog.Take(Math.Min(DefaultVisibleCount, catalog.Count)).ToList();

            var wanted = NormalizeSavedKeys(savedKeys, catalog);
            if (wanted.Count == 0)
                return Array.Empty<DashboardQuickAction>();

            var map = catalog
                .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return wanted
                .Where(map.ContainsKey)
                .Select(k => map[k])
                .ToList();
        }

        public static IReadOnlySet<string> ParseSavedKeys(string? savedKeys, IReadOnlyList<DashboardQuickAction> catalog)
        {
            if (savedKeys == null)
            {
                return Resolve(catalog, null)
                    .Select(a => a.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            return NormalizeSavedKeys(savedKeys, catalog)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> NormalizeSavedKeys(string savedKeys, IReadOnlyList<DashboardQuickAction> catalog)
        {
            var catalogKeys = catalog
                .Select(c => c.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var normalized = new List<string>();
            foreach (var raw in savedKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (catalogKeys.Contains(raw))
                {
                    normalized.Add(raw);
                    continue;
                }

                if (IsDashboardLegacyKey(raw))
                {
                    normalized.Add(DashboardKey);
                    continue;
                }

                foreach (var part in raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (catalogKeys.Contains(part))
                    {
                        normalized.Add(part);
                        break;
                    }
                }
            }

            return normalized
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsDashboardLegacyKey(string raw) =>
            raw.Equals(DashboardKey, StringComparison.OrdinalIgnoreCase)
            || raw.Equals("home|dashboard", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Dashboard|Index", StringComparison.OrdinalIgnoreCase);

        private static DashboardQuickAction ToQuickAction(
            KRSDealerManagement.Shared.Constants.MenuItemDefinition item,
            string groupLabel,
            UrlBuilder urlBuilder,
            ref int iconIndex)
        {
            var icon = string.IsNullOrWhiteSpace(item.Icon) ? "bi-circle" : item.Icon;
            var iconBg = IconBgs[iconIndex % IconBgs.Length];
            iconIndex++;

            return new DashboardQuickAction(
                item.Key,
                item.Name,
                urlBuilder(item.Controller, item.Action, item.RouteValues),
                icon,
                iconBg,
                groupLabel);
        }
    }
}

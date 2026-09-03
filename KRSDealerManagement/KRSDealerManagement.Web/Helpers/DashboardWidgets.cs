using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Web.Helpers
{
    public sealed record DashboardWidgetItem(string Key, string Label, string Group);

    public sealed class DashboardWidgetSection
    {
        public required string Group { get; init; }
        public string? HelpText { get; init; }
        public string? IconClass { get; init; }
        public IReadOnlyList<string> Keys { get; init; } = Array.Empty<string>();
    }

    public sealed class DashboardWidgetsContext
    {
        public bool IsAdmin { get; init; }
        public bool IsSubdealer { get; init; }
        public bool IsBranchManager { get; init; }
        public bool CanViewOrders { get; init; }
        public bool CanViewReturns { get; init; }
        public bool CanViewPayments { get; init; }
        public bool CanViewCommissions { get; init; }
        public bool CanViewDealerStock { get; init; }
        public bool CanViewShowroomStock { get; init; }
        public bool ShowBookingCounts { get; init; }
        public bool ShowStaffOnlyBookingStages { get; init; }
        public bool CanViewRtoSubsidyProgress { get; init; }
    }

    public static class DashboardWidgets
    {
        public static IReadOnlyList<DashboardWidgetItem> GetCatalog(DashboardWidgetsContext ctx)
        {
            var items = new List<DashboardWidgetItem>();

            if (ctx.CanViewOrders)
                items.Add(new(DashboardWidgetKeys.PendingOrders, "Pending Orders", DashboardWidgetGroups.PendingActions));
            if (ctx.CanViewReturns)
                items.Add(new(DashboardWidgetKeys.PendingReturns, "Pending Returns", DashboardWidgetGroups.PendingActions));
            if (ctx.CanViewPayments)
                items.Add(new(DashboardWidgetKeys.PendingPayments, "Pending Payments", DashboardWidgetGroups.PendingActions));
            if (ctx.CanViewCommissions)
                items.Add(new(DashboardWidgetKeys.PendingCommissions, "Pending Commissions", DashboardWidgetGroups.PendingActions));

            if (!ctx.IsAdmin && ctx.IsBranchManager)
            {
                if (ctx.CanViewDealerStock)
                    items.Add(new(DashboardWidgetKeys.DealerStock, "Dealer Stock", DashboardWidgetGroups.Stock));
                if (ctx.CanViewShowroomStock)
                    items.Add(new(DashboardWidgetKeys.ShowroomStock, "Subdealer Stock", DashboardWidgetGroups.Stock));
            }

            if (ctx.ShowBookingCounts)
            {
                items.Add(new(DashboardWidgetKeys.BookedToCustomer, "Booked to Customer", DashboardWidgetGroups.ManageVehicles));
                items.Add(new(DashboardWidgetKeys.PaperReceived, "Paper Received", DashboardWidgetGroups.ManageVehicles));
                items.Add(new(DashboardWidgetKeys.Invoiced, "Invoiced", DashboardWidgetGroups.ManageVehicles));
                items.Add(new(DashboardWidgetKeys.InsuranceCreated, "Insurance Created", DashboardWidgetGroups.ManageVehicles));
                items.Add(new(DashboardWidgetKeys.RtoRequested, "RTO Requested", DashboardWidgetGroups.ManageVehicles));

                if (ctx.CanViewRtoSubsidyProgress)
                {
                    items.Add(new(DashboardWidgetKeys.SubsidyIdPending, "Subsidy ID Pending", DashboardWidgetGroups.ManageVehicles));
                    items.Add(new(DashboardWidgetKeys.SubsidyDocsPending, "Subsidy Docs Pending", DashboardWidgetGroups.ManageVehicles));
                    items.Add(new(DashboardWidgetKeys.Registered, "Registered", DashboardWidgetGroups.ManageVehicles));
                }
            }

            return items;
        }

        public static IReadOnlyList<string> ResolveOrder(IReadOnlyList<DashboardWidgetItem> catalog, string? savedKeys)
        {
            var defaultOrder = catalog.Select(c => c.Key).ToList();
            if (catalog.Count == 0) return defaultOrder;

            if (string.IsNullOrWhiteSpace(savedKeys))
                return defaultOrder;

            var catalogKeySet = catalog
                .Select(c => c.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var ordered = new List<string>();
            foreach (var raw in savedKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (catalogKeySet.Contains(raw) && !ordered.Contains(raw, StringComparer.OrdinalIgnoreCase))
                    ordered.Add(raw);
            }

            foreach (var key in defaultOrder)
            {
                if (!ordered.Contains(key, StringComparer.OrdinalIgnoreCase))
                    ordered.Add(key);
            }

            return ordered;
        }

        public static IReadOnlyList<DashboardWidgetSection> GroupBySections(
            IReadOnlyList<DashboardWidgetItem> catalog,
            IReadOnlyList<string> orderedKeys)
        {
            var labelByKey = catalog.ToDictionary(c => c.Key, c => c, StringComparer.OrdinalIgnoreCase);
            var sections = new List<DashboardWidgetSection>();
            List<string>? currentKeys = null;
            string? currentGroup = null;
            string? currentHelp = null;
            string? currentIcon = null;

            void FlushSection()
            {
                if (currentGroup == null || currentKeys == null || currentKeys.Count == 0)
                    return;

                sections.Add(new DashboardWidgetSection
                {
                    Group = currentGroup,
                    HelpText = currentHelp,
                    IconClass = currentIcon,
                    Keys = currentKeys
                });
            }

            foreach (var key in orderedKeys)
            {
                if (!labelByKey.TryGetValue(key, out var item))
                    continue;

                if (!string.Equals(currentGroup, item.Group, StringComparison.OrdinalIgnoreCase))
                {
                    FlushSection();
                    currentGroup = item.Group;
                    currentHelp = GetSectionHelp(item.Group);
                    currentIcon = GetSectionIcon(item.Group);
                    currentKeys = new List<string>();
                }

                currentKeys!.Add(key);
            }

            FlushSection();
            return sections;
        }

        public static string? GetSectionHelp(string group) => group switch
        {
            DashboardWidgetGroups.PendingActions =>
                "Items that need attention. The number is how many are waiting right now.",
            DashboardWidgetGroups.Stock =>
                DashboardWidgetHelp.StockSection,
            DashboardWidgetGroups.ManageVehicles =>
                DashboardWidgetHelp.ManageVehiclesSection,
            _ => null
        };

        public static string? GetSectionIcon(string group) => group switch
        {
            DashboardWidgetGroups.PendingActions => "bi-hourglass-split",
            DashboardWidgetGroups.Stock => "bi-boxes",
            DashboardWidgetGroups.ManageVehicles => "bi-truck-front",
            _ => "bi-grid"
        };
    }
}

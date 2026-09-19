using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;
using Microsoft.AspNetCore.Http;

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

        public static DashboardWidgetsContext Build(ISession session)
        {
            var isAdmin = SessionHelper.IsSystemAdmin(session);
            var isSubdealer = SessionHelper.IsSubdealer(session);

            return new DashboardWidgetsContext
            {
                IsAdmin = isAdmin,
                IsSubdealer = isSubdealer,
                IsBranchManager = SessionHelper.IsBranchManager(session),
                CanViewOrders = isSubdealer || SessionHelper.HasMenuAccess(session, StaffMenuAccess.Orders),
                CanViewReturns = isSubdealer || SessionHelper.HasMenuAccess(session, StaffMenuAccess.Returns),
                CanViewPayments = isSubdealer || SessionHelper.HasMenuAccess(session, StaffMenuAccess.Payments),
                CanViewCommissions = isSubdealer || isAdmin,
                CanViewDealerStock = SessionHelper.HasMenuAccess(session, StaffMenuAccess.DealerStock),
                CanViewShowroomStock = SessionHelper.HasMenuAccess(session, StaffMenuAccess.ShowroomStock)
            };
        }
    }

    public static class DashboardWidgets
    {
        private static readonly (string WidgetKey, string Label, string MenuKey)[] BookingStageWidgets =
        {
            (DashboardWidgetKeys.BookedToCustomer, "Booked to Customer", StaffMenuAccess.BookedToCustomerView),
            (DashboardWidgetKeys.PaperReceived, "Paper Received", StaffMenuAccess.BookingPaperReceived),
            (DashboardWidgetKeys.Invoiced, "Invoiced", StaffMenuAccess.BookingInvoiced),
            (DashboardWidgetKeys.InsuranceCreated, "Insurance Created", StaffMenuAccess.BookingInsuranceCreated),
            (DashboardWidgetKeys.RtoRequested, "RTO Requested", StaffMenuAccess.BookingRtoRequested),
            (DashboardWidgetKeys.SubsidyIdPending, "Subsidy ID Pending", StaffMenuAccess.BookingSubsidyIdPending),
            (DashboardWidgetKeys.SubsidyDocsPending, "Subsidy Docs Pending", StaffMenuAccess.BookingSubsidyDocsPending),
            (DashboardWidgetKeys.Registered, "Registered", StaffMenuAccess.BookingRegistered)
        };

        public static IReadOnlyList<DashboardWidgetItem> GetCatalog(DashboardWidgetsContext ctx, ISession session)
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

            foreach (var (widgetKey, label, menuKey) in BookingStageWidgets)
            {
                if (CanShowBookingWidget(ctx, session, menuKey))
                    items.Add(new(widgetKey, label, DashboardWidgetGroups.ManageVehicles));
            }

            if (ctx.CanViewDealerStock)
                items.Add(new(DashboardWidgetKeys.DealerStock, "Dealer Stock", DashboardWidgetGroups.Stock));
            if (ctx.CanViewShowroomStock)
                items.Add(new(DashboardWidgetKeys.ShowroomStock, "Subdealer Stock", DashboardWidgetGroups.Stock));

            return items;
        }

        private static bool CanShowBookingWidget(DashboardWidgetsContext ctx, ISession session, string menuKey)
        {
            if (ctx.IsAdmin)
                return true;

            if (ctx.IsSubdealer)
                return SessionHelper.HasMenuAccess(session, MenuKeys.VehiclesBookingStages);

            return SessionHelper.HasMenuAccess(session, menuKey);
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

            var sectionOrder = new[]
            {
                DashboardWidgetGroups.PendingActions,
                DashboardWidgetGroups.ManageVehicles,
                DashboardWidgetGroups.Stock
            };

            return sections
                .OrderBy(s =>
                {
                    var idx = Array.FindIndex(sectionOrder, g => string.Equals(g, s.Group, StringComparison.OrdinalIgnoreCase));
                    return idx < 0 ? sectionOrder.Length : idx;
                })
                .ToList();
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

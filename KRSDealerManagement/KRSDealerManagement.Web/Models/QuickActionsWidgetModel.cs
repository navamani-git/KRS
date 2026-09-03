using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Models
{
    public class QuickActionsWidgetModel
    {
        public IReadOnlyList<DashboardQuickAction> QuickActions { get; init; } = Array.Empty<DashboardQuickAction>();
        public IReadOnlyList<DashboardQuickAction> Catalog { get; init; } = Array.Empty<DashboardQuickAction>();
        public IReadOnlySet<string> SelectedKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}

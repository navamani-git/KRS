using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Models
{
    public class DashboardWidgetsCustomizeModel
    {
        public IReadOnlyList<DashboardWidgetSection> Sections { get; set; } = Array.Empty<DashboardWidgetSection>();
        public IReadOnlyList<DashboardWidgetItem> Catalog { get; set; } = Array.Empty<DashboardWidgetItem>();
        public IReadOnlyList<string> OrderedKeys { get; set; } = Array.Empty<string>();
        public bool CanCustomize => Catalog.Count > 1;
    }
}

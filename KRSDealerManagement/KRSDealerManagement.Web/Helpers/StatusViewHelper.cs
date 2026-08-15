using KRSDealerManagement.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KRSDealerManagement.Web.Helpers
{
    public static class StatusViewHelper
    {
        public static IEnumerable<SelectListItem> ToSelectList(
            IEnumerable<StatusLookup> statuses,
            int? selected = null,
            string allText = "All Status")
        {
            var items = new List<SelectListItem>
            {
                new() { Value = "", Text = allText, Selected = !selected.HasValue }
            };

            foreach (var s in statuses.OrderBy(x => x.SortOrder))
            {
                items.Add(new SelectListItem
                {
                    Value = s.StatusValue.ToString(),
                    Text = s.StatusName,
                    Selected = selected.HasValue && selected.Value == s.StatusValue
                });
            }

            return items;
        }

        public static string BadgeClass(IEnumerable<StatusLookup>? statuses, int statusValue, string fallback = "bg-secondary")
        {
            var match = statuses?.FirstOrDefault(s => s.StatusValue == statusValue);
            return string.IsNullOrWhiteSpace(match?.BadgeClass) ? fallback : match!.BadgeClass;
        }

        public static string DisplayName(IEnumerable<StatusLookup>? statuses, int statusValue, string fallback = "Unknown")
        {
            var match = statuses?.FirstOrDefault(s => s.StatusValue == statusValue);
            return string.IsNullOrWhiteSpace(match?.StatusName) ? fallback : match!.StatusName;
        }
    }
}

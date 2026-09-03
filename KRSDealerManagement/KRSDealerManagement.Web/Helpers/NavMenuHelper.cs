using KRSDealerManagement.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KRSDealerManagement.Web.Helpers
{
    public static class NavMenuHelper
    {
        /// <summary>Every sidebar link the user can open (same rules as _Layout.cshtml).</summary>
        public static IEnumerable<(string GroupName, MenuItemDefinition Item)> EnumerateVisibleMenuItems(ISession session)
        {
            if (SessionHelper.IsStaff(session))
            {
                foreach (var group in StaffMenuAccess.GetStaffMenuGroups())
                {
                    foreach (var item in group.Children
                                 .Where(c => SessionHelper.HasMenuAccess(session, c.Key))
                                 .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        yield return (group.ParentName, item);
                    }
                }
            }

            if (SessionHelper.IsSubdealer(session))
            {
                foreach (var group in MenuKeys.GetSubdealerMenuGroups())
                {
                    foreach (var item in group.Children
                                 .Where(c => SessionHelper.HasMenuAccess(session, c.Key))
                                 .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        yield return (group.ParentName, item);
                    }
                }
            }
        }

        public static string ActiveClass(string? currentController, string? currentAction, string controller, string? action = null)
        {
            if (!string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase))
                return "";

            if (action != null && !string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase))
                return "";

            return "active";
        }

        public static string ActiveClassAnyAction(string? currentController, string? currentAction, string controller, params string[] actions)
        {
            if (!string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase))
                return "";

            if (actions.Length > 0 && !actions.Any(a => string.Equals(currentAction, a, StringComparison.OrdinalIgnoreCase)))
                return "";

            return "active";
        }

        public static string ActiveClassForMenuItem(
            string? currentController,
            string? currentAction,
            KRSDealerManagement.Shared.Constants.MenuItemDefinition item,
            Microsoft.AspNetCore.Http.HttpRequest? request = null)
        {
            if (item.RouteValues is { Count: > 0 })
            {
                if (!string.Equals(currentController, item.Controller, StringComparison.OrdinalIgnoreCase))
                    return "";

                var actionMatch = item.Actions is { Count: > 0 }
                    ? item.Actions.Any(a => string.Equals(currentAction, a, StringComparison.OrdinalIgnoreCase))
                    : string.Equals(currentAction, item.Action, StringComparison.OrdinalIgnoreCase);

                if (!actionMatch)
                    return "";

                if (request != null && !MenuItemRouteMatches(request, item))
                    return "";

                return "active";
            }

            // Pipeline list pages (Index + status) must not activate the full-process menu item
            if (request != null
                && !string.IsNullOrWhiteSpace(request.Query["status"])
                && string.Equals(currentAction, "Index", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Action, "Process", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            if (item.Actions is { Count: > 0 })
                return ActiveClassAnyAction(currentController, currentAction, item.Controller, item.Actions.ToArray());

            return ActiveClass(currentController, currentAction, item.Controller, item.Action);
        }

        public static bool IsMenuItemActive(
            string? currentController,
            string? currentAction,
            KRSDealerManagement.Shared.Constants.MenuItemDefinition item,
            Microsoft.AspNetCore.Http.HttpRequest? request = null)
            => ActiveClassForMenuItem(currentController, currentAction, item, request) == "active";

        /// <summary>
        /// Whether a sidebar parent group should expand/highlight for the current page.
        /// </summary>
        public static bool IsMenuGroupOpen(
            string parentKey,
            string? currentController,
            string? currentAction,
            Microsoft.AspNetCore.Http.HttpRequest request,
            IEnumerable<KRSDealerManagement.Shared.Constants.MenuItemDefinition> visibleItems)
        {
            if (visibleItems.Any(c => IsMenuItemActive(currentController, currentAction, c, request)))
                return true;

            return false;
        }

        public static bool MenuItemRouteMatches(
            Microsoft.AspNetCore.Http.HttpRequest request,
            KRSDealerManagement.Shared.Constants.MenuItemDefinition item)
        {
            if (item.RouteValues is not { Count: > 0 })
                return true;

            foreach (var kv in item.RouteValues)
            {
                var actual = request.Query[kv.Key].ToString();
                var expected = kv.Value?.ToString() ?? "";
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }
}

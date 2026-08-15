using Microsoft.AspNetCore.Mvc.Rendering;

namespace KRSDealerManagement.Web.Helpers
{
    public static class NavMenuHelper
    {
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

        public static string ActiveClassForMenuItem(string? currentController, string? currentAction, KRSDealerManagement.Shared.Constants.MenuItemDefinition item)
        {
            if (item.Actions is { Count: > 0 })
                return ActiveClassAnyAction(currentController, currentAction, item.Controller, item.Actions.ToArray());

            return ActiveClass(currentController, currentAction, item.Controller, item.Action);
        }
    }
}

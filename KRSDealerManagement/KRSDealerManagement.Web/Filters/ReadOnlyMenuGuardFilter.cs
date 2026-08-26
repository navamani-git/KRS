using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Filters
{
    /// <summary>
    /// Blocks write actions when the current menu is configured as read-only.
    /// Export* actions remain allowed.
    /// </summary>
    public class ReadOnlyMenuGuardFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (!SessionHelper.IsAuthenticated(session) || SessionHelper.IsSystemAdmin(session))
                return;

            var method = context.HttpContext.Request.Method;
            if (!HttpMethods.IsPost(method) && !HttpMethods.IsPut(method) && !HttpMethods.IsDelete(method))
                return;

            var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var act) ? act : "";
            if (action.StartsWith("Export", StringComparison.OrdinalIgnoreCase))
                return;

            var controller = context.ActionDescriptor.RouteValues.TryGetValue("controller", out var ctrl) ? ctrl : "";
            if (!StaffMenuAccess.TryResolveMenuKey(controller ?? "", action ?? "", out var menuKey))
                return;

            if (SessionHelper.CanWriteMenu(session, menuKey))
                return;

            if (SessionHelper.HasMenuAccess(session, menuKey))
            {
                if (context.Controller is Controller mvcController)
                    mvcController.TempData["Error"] = "This screen is read-only for your role. Export is still allowed.";
                context.Result = new RedirectResult(context.HttpContext.Request.Headers.Referer.ToString());
                if (string.IsNullOrWhiteSpace(context.HttpContext.Request.Headers.Referer))
                    context.Result = new RedirectToActionResult("Index", "Dashboard", null);
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

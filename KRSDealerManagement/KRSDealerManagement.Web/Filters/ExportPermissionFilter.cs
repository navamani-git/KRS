using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Filters
{
    /// <summary>Blocks Excel export actions when the user is not allowed to export.</summary>
    public class ExportPermissionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (!SessionHelper.IsAuthenticated(session) || SessionHelper.IsSystemAdmin(session))
                return;

            var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var act) ? act : "";
            if (!action.StartsWith("Export", StringComparison.OrdinalIgnoreCase))
                return;

            var controller = context.ActionDescriptor.RouteValues.TryGetValue("controller", out var ctrl) ? ctrl : "";
            StaffMenuAccess.TryResolveMenuKey(controller ?? "", action ?? "", out var menuKey);

            if (!SessionHelper.CanExport(session)
                || (!string.IsNullOrWhiteSpace(menuKey) && !SessionHelper.CanExportMenu(session, menuKey)))
            {
                if (context.Controller is Controller mvcController)
                    mvcController.TempData["Error"] = "Excel export is disabled for your account.";
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

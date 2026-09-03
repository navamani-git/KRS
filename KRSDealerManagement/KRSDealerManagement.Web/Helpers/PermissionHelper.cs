using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Application.Queries;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>Legacy permission helper — prefers SessionHelper.HasMenuAccess (RoleMenus).</summary>
    public static class PermissionHelper
    {
        public static bool HasAccess(Microsoft.AspNetCore.Http.ISession session, string menuKey)
            => SessionHelper.HasMenuAccess(session, menuKey);

        public static async Task RefreshSessionAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, IMediator mediator)
        {
            var userId = SessionHelper.GetUserId(httpContext.Session);
            if (!userId.HasValue) return;

            var ctx = await mediator.Send(new GetUserAccessContextQuery { UserId = userId.Value });
            if (ctx == null) return;

            SessionHelper.SetUserSession(
                httpContext.Session,
                userId.Value,
                SessionHelper.GetUsername(httpContext.Session) ?? "",
                SessionHelper.GetFullName(httpContext.Session) ?? "",
                SessionHelper.GetUserRole(httpContext.Session) ?? 0,
                ctx.RoleName,
                ctx.RoleCode,
                ctx.DealershipId,
                ctx.DealershipName,
                ctx.SubDealerId,
                ctx.AccessibleMenuKeys,
                ctx.MenuAccess,
                ctx.CanExport,
                ctx.QuickActionKeys,
                ctx.DashboardWidgetKeys);
        }
    }
}

namespace KRSDealerManagement.Web.Filters
{
    /// <summary>Requires a RoleMenus MenuKey (or System Admin).</summary>
    public class AuthorizeMenuAttribute : ActionFilterAttribute
    {
        private readonly string _menuKey;

        public AuthorizeMenuAttribute(string menuKey) => _menuKey = menuKey;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (!SessionHelper.IsAuthenticated(session))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (!SessionHelper.HasMenuAccess(session, _menuKey))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>Requires any one of the given RoleMenus MenuKeys (or System Admin).</summary>
    public class AuthorizeMenuAnyAttribute : ActionFilterAttribute
    {
        private readonly string[] _menuKeys;

        public AuthorizeMenuAnyAttribute(params string[] menuKeys) => _menuKeys = menuKeys;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (!SessionHelper.IsAuthenticated(session))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (!_menuKeys.Any(k => SessionHelper.HasMenuAccess(session, k)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Filters
{
    /// <summary>
    /// Legacy numeric UserRole gate. Prefer <see cref="AuthorizeMenuAttribute"/> / <see cref="AuthorizeMenuAnyAttribute"/>.
    /// Kept only so old references compile; do not use on new screens.
    /// </summary>
    [Obsolete("Use AuthorizeMenu / AuthorizeMenuAny. Access is driven by RoleMenus, not hardcoded role ids.")]
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly int[] _allowedRoles;

        public AuthorizeRoleAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (!SessionHelper.IsAuthenticated(session))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var userRole = SessionHelper.GetUserRole(session);
            if (userRole == null || !_allowedRoles.Contains(userRole.Value))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

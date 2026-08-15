using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Filters
{
    /// <summary>
    /// Authorization filter for role-based access control
    /// Usage: [AuthorizeRole(1)] for Admin only, [AuthorizeRole(1, 2)] for Admin and Subdealer
    /// </summary>
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

            // Check if user is authenticated
            if (!SessionHelper.IsAuthenticated(session))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Check if user has required role
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

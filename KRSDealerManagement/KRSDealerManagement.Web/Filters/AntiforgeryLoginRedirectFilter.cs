using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KRSDealerManagement.Web.Filters
{
    /// <summary>
    /// Stale anti-forgery tokens (idle / app-pool recycle) become HTTP 400.
    /// Redirect to login instead of the browser error page.
    /// </summary>
    public sealed class AntiforgeryLoginRedirectFilter : IAlwaysRunResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (!IsAntiforgeryFailure(context.Result))
                return;

            var accept = context.HttpContext.Request.Headers.Accept.ToString();
            var ajax = string.Equals(
                context.HttpContext.Request.Headers.XRequestedWith,
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            if (ajax || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    loginRequired = true,
                    redirectUrl = "/Account/Login"
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            context.Result = new RedirectToActionResult("Login", "Account", null);
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        private static bool IsAntiforgeryFailure(IActionResult? result)
        {
            if (result == null)
                return false;

            var typeName = result.GetType().FullName ?? result.GetType().Name;
            return typeName.Contains("AntiforgeryValidationFailed", StringComparison.Ordinal);
        }
    }
}

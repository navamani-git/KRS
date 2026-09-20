using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Middleware
{
    /// <summary>
    /// After the session cookie is gone or the worker recycled, POSTs hit anti-forgery
    /// and return HTTP 400. Send the user to login instead of Chrome's error page.
    /// </summary>
    public sealed class ExpiredSessionRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public ExpiredSessionRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsAnonymousPath(context) && !SessionHelper.IsAuthenticated(context.Session))
            {
                if (WantsJson(context))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        loginRequired = true,
                        redirectUrl = "/Account/Login"
                    });
                    return;
                }

                context.Response.Redirect("/Account/Login");
                return;
            }

            await _next(context);
        }

        private static bool IsAnonymousPath(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (string.IsNullOrEmpty(path) || path == "/")
                return true;

            return path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Account/SetFontSize", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase);
        }

        private static bool WantsJson(HttpContext context)
        {
            if (string.Equals(context.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return true;

            var accept = context.Request.Headers.Accept.ToString();
            return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}

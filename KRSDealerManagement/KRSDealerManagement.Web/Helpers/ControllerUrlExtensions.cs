using KRSDealerManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KRSDealerManagement.Web.Helpers
{
    public static class ControllerUrlExtensions
    {
        /// <summary>
        /// Redirect with encrypted query string (id and filters are not exposed in the URL path).
        /// </summary>
        public static RedirectResult RedirectEncrypted(
            this ControllerBase controller,
            string actionName,
            object? routeValues = null,
            string? controllerName = null)
        {
            var crypto = controller.HttpContext.RequestServices.GetRequiredService<IQueryStringCrypto>();
            var url = QueryStringUrlHelper.EncryptedAction(
                controller.Url, crypto, actionName, routeValues, controllerName);
            return new RedirectResult(url);
        }
    }
}

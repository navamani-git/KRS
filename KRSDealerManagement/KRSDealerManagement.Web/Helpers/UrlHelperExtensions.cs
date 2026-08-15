using KRSDealerManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Build action URL with all parameters (including id) in encrypted ?q=...
        /// </summary>
        public static string EAction(
            this IUrlHelper url,
            IQueryStringCrypto crypto,
            string action,
            object? values = null,
            string? controller = null)
            => QueryStringUrlHelper.EncryptedAction(url, crypto, action, values, controller);
    }
}

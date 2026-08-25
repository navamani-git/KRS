using KRSDealerManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Central helper for building URLs with encrypted query strings in views and controllers.
    /// </summary>
    public static class QueryStringUrlHelper
    {
        private static readonly HashSet<string> RouteKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "controller", "action", "area"
        };

        /// <summary>
        /// Build an action URL with route values in the path and other values in encrypted query string.
        /// </summary>
        public static string EncryptedAction(
            IUrlHelper url,
            IQueryStringCrypto crypto,
            string action,
            object? values = null,
            string? controller = null,
            string? protocol = null,
            string? host = null,
            string? fragment = null)
        {
            var all = new RouteValueDictionary(values);
            var route = new RouteValueDictionary();
            var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in all)
            {
                if (RouteKeys.Contains(kv.Key))
                    route[kv.Key] = kv.Value;
                else if (kv.Value != null)
                    query[kv.Key] = kv.Value.ToString();
            }

            if (!string.IsNullOrEmpty(controller))
                route["controller"] = controller;
            route["action"] = action;

            var path = url.Action(action, controller, route, protocol, host, fragment) ?? "/";
            return crypto.AppendToPath(path, query);
        }

        /// <summary>
        /// Build encrypted query string from current request filters, optionally overriding page.
        /// </summary>
        public static string BuildPagedQuery(HttpContext httpContext, IQueryStringCrypto crypto, int targetPage, int? pageSize = null)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in httpContext.Request.Query)
            {
                if (string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (pageSize.HasValue && string.Equals(kv.Key, "pageSize", StringComparison.OrdinalIgnoreCase))
                    continue;
                dict[kv.Key] = kv.Value.ToString();
            }

            dict["page"] = targetPage.ToString();
            if (pageSize.HasValue)
                dict["pageSize"] = pageSize.Value.ToString();
            return crypto.BuildQueryString(dict);
        }
    }
}

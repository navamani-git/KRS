using KRSDealerManagement.Web.Services;
using Microsoft.Extensions.Primitives;

namespace KRSDealerManagement.Web.Middleware
{
    /// <summary>
    /// Encrypts/decrypts all URL parameters: query strings and path ids (e.g. /Details/3 → /Details?q=...).
    /// Must run before UseRouting().
    /// </summary>
    public class QueryStringEncryptionMiddleware
    {
        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/QueryString/Pack"
        };

        private readonly RequestDelegate _next;
        private readonly IQueryStringCrypto _crypto;
        private readonly ILogger<QueryStringEncryptionMiddleware> _logger;

        public QueryStringEncryptionMiddleware(
            RequestDelegate next,
            IQueryStringCrypto crypto,
            ILogger<QueryStringEncryptionMiddleware> logger)
        {
            _next = next;
            _crypto = crypto;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ExcludedPaths.Contains(context.Request.Path.Value ?? string.Empty))
            {
                await _next(context);
                return;
            }

            var query = context.Request.Query;

            // Decrypt ?q= on any verb (GET navigation and POST form actions using EAction)
            if (query.ContainsKey(_crypto.ParamName))
            {
                ApplyDecryptedQuery(context, query[_crypto.ParamName].ToString());
                await _next(context);
                return;
            }

            // POST/PUT/PATCH/DELETE: promote /Controller/Action/123 path id into query for conventional routing
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                if (TryExtractPathId(context.Request.Path, out var controller, out var action, out var pathId))
                {
                    context.Request.Path = new PathString($"/{controller}/{action}");
                    var newQuery = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["id"] = pathId
                    };
                    foreach (var kv in query)
                        newQuery[kv.Key] = kv.Value;
                    context.Request.Query = new QueryCollection(newQuery);
                }

                await _next(context);
                return;
            }

            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (TryExtractPathId(context.Request.Path, out var getController, out var getAction, out var getPathId))
                values["id"] = getPathId;

            foreach (var kv in query)
                values[kv.Key] = kv.Value.ToString();

            if (values.Count > 0)
            {
                var path = !string.IsNullOrWhiteSpace(getController) && !string.IsNullOrWhiteSpace(getAction)
                    ? $"/{getController}/{getAction}"
                    : context.Request.Path.Value ?? "/";

                context.Response.Redirect(path + _crypto.BuildQueryString(values), permanent: false);
                return;
            }

            await _next(context);
        }

        /// <summary>Detect legacy /Controller/Action/123 URLs.</summary>
        private static bool TryExtractPathId(PathString path, out string? controller, out string? action, out string pathId)
        {
            controller = null;
            action = null;
            pathId = string.Empty;

            var segments = path.Value?.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (segments.Length != 3)
                return false;

            if (!LooksLikeEntityId(segments[2]))
                return false;

            controller = segments[0];
            action = segments[1];
            pathId = segments[2];
            return true;
        }

        private static bool LooksLikeEntityId(string segment)
            => int.TryParse(segment, out _) || Guid.TryParse(segment, out _);

        private void ApplyDecryptedQuery(HttpContext context, string token)
        {
            var decrypted = _crypto.Decrypt(token);
            if (decrypted.Count == 0)
            {
                _logger.LogWarning("Failed to decrypt query string on {Path}", context.Request.Path);
                return;
            }

            var newQuery = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in decrypted)
                newQuery[kv.Key] = kv.Value;

            context.Request.Query = new QueryCollection(newQuery);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Web.Services;

namespace KRSDealerManagement.Web.Controllers
{
    /// <summary>
    /// Supports AJAX callers that need server-side query string encryption.
    /// </summary>
    public class QueryStringController : Controller
    {
        private readonly IQueryStringCrypto _crypto;

        public QueryStringController(IQueryStringCrypto crypto) => _crypto = crypto;

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Pack([FromBody] Dictionary<string, string>? values)
        {
            values ??= new Dictionary<string, string>();
            var q = _crypto.Encrypt(values.ToDictionary(kv => kv.Key, kv => (string?)kv.Value));
            if (string.IsNullOrEmpty(q))
                return BadRequest(new { success = false, message = "No values to encrypt." });

            return Json(new { success = true, q });
        }
    }
}

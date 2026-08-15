using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace KRSDealerManagement.Web.Services
{
    public class QueryStringCrypto : IQueryStringCrypto
    {
        public const string Purpose = "KRSDealerManagement.QueryString.v1";
        private readonly IDataProtector _protector;

        public QueryStringCrypto(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(Purpose);
        }

        public string ParamName => "q";

        public string Encrypt(IReadOnlyDictionary<string, string?> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            var filtered = values
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!, StringComparer.OrdinalIgnoreCase);

            if (filtered.Count == 0)
                return string.Empty;

            var json = JsonSerializer.Serialize(filtered);
            return _protector.Protect(json);
        }

        public IReadOnlyDictionary<string, string> Decrypt(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var json = _protector.Unprotect(token);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public string BuildQueryString(IReadOnlyDictionary<string, string?> values)
        {
            var token = Encrypt(values);
            return string.IsNullOrEmpty(token)
                ? string.Empty
                : "?" + ParamName + "=" + Uri.EscapeDataString(token);
        }

        public string AppendToPath(string path, IReadOnlyDictionary<string, string?> values)
        {
            var query = BuildQueryString(values);
            return string.IsNullOrEmpty(query) ? path : path + query;
        }
    }
}

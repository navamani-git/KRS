namespace KRSDealerManagement.Web.Services
{
    /// <summary>
    /// Single entry point for encrypting/decrypting URL query strings across the app.
    /// </summary>
    public interface IQueryStringCrypto
    {
        /// <summary>Query parameter name used for encrypted payloads.</summary>
        string ParamName { get; }

        /// <summary>Encrypt key/value pairs into a URL-safe token.</summary>
        string Encrypt(IReadOnlyDictionary<string, string?> values);

        /// <summary>Decrypt a token back into key/value pairs.</summary>
        IReadOnlyDictionary<string, string> Decrypt(string token);

        /// <summary>Build encrypted query string, e.g. ?q=AbCd...</summary>
        string BuildQueryString(IReadOnlyDictionary<string, string?> values);

        /// <summary>Merge encrypted query onto a path (path may already contain a route id).</summary>
        string AppendToPath(string path, IReadOnlyDictionary<string, string?> values);
    }
}

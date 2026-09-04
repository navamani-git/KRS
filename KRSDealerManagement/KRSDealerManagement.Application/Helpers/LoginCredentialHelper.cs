using Microsoft.AspNetCore.Identity;

namespace KRSDealerManagement.Application.Helpers
{
    public static class LoginCredentialHelper
    {
        private static readonly PasswordHasher<string> PasswordHasher = new();

        public static bool VerifyPassword(string? storedHash, string enteredPassword)
        {
            storedHash = storedHash?.Trim() ?? string.Empty;
            enteredPassword = enteredPassword?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(enteredPassword))
                return false;

            if (storedHash.StartsWith("AQAA", StringComparison.Ordinal))
            {
                var result = PasswordHasher.VerifyHashedPassword(null!, storedHash, enteredPassword);
                return result == PasswordVerificationResult.Success
                    || result == PasswordVerificationResult.SuccessRehashNeeded;
            }

            return string.Equals(storedHash, enteredPassword, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeUsername(string username)
            => username.Trim().ToLowerInvariant();
    }
}

namespace KRSDealerManagement.Application.Helpers
{
    public static class StaffRoleDisplayHelper
    {
        public static string ResolveDisplayName(
            string roleName,
            string roleCode,
            string? templateName,
            string? dealershipName)
        {
            if (!string.IsNullOrWhiteSpace(roleName)
                && !string.Equals(roleName.Trim(), roleCode.Trim(), StringComparison.OrdinalIgnoreCase)
                && !LooksLikeInternalCode(roleName))
            {
                return roleName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(templateName))
            {
                if (!string.IsNullOrWhiteSpace(dealershipName)
                    && !dealershipName.Equals("All locations (shared)", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{templateName.Trim()} ({dealershipName.Trim()})";
                }

                return templateName.Trim();
            }

            return HumanizeCode(string.IsNullOrWhiteSpace(roleName) ? roleCode : roleName);
        }

        private static bool LooksLikeInternalCode(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.Length == 0)
                return false;

            return trimmed.Contains('_', StringComparison.Ordinal)
                && trimmed.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '_');
        }

        private static string HumanizeCode(string code)
        {
            return string.Join(' ',
                code.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word =>
                    {
                        if (word.Length == 0)
                            return word;

                        if (word.All(char.IsDigit))
                            return word;

                        return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
                    }));
        }
    }
}

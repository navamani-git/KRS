namespace KRSDealerManagement.Shared.Extensions
{
    /// <summary>
    /// String extension methods for common operations
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Check if string is null or empty
        /// </summary>
        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Check if string has content
        /// </summary>
        public static bool HasValue(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Truncate string to specified length with ellipsis
        /// </summary>
        public static string Truncate(this string value, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= length ? value : value.Substring(0, length - 3) + "...";
        }

        /// <summary>
        /// Convert string to title case
        /// </summary>
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var titleCase = System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(value.ToLower());
            return titleCase;
        }

        /// <summary>
        /// Convert string to slug (URL-friendly format)
        /// </summary>
        public static string ToSlug(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            value = value.ToLower().Trim();
            value = System.Text.RegularExpressions.Regex.Replace(value, @"[^\w\s-]", "");
            value = System.Text.RegularExpressions.Regex.Replace(value, @"[\s_-]+", "-");
            value = System.Text.RegularExpressions.Regex.Replace(value, @"^-+|-+$", "");

            return value;
        }

        /// <summary>
        /// Mask sensitive information (show only last 4 characters)
        /// </summary>
        public static string MaskSensitive(this string value, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
                return value;

            int maskLength = value.Length - visibleChars;
            return new string('*', maskLength) + value.Substring(maskLength);
        }

        /// <summary>
        /// Format currency string (e.g., "1000" -> "₹1,000.00")
        /// </summary>
        public static string FormatCurrency(this decimal value, string currency = "₹")
        {
            return $"{currency}{value:N2}";
        }
    }
}

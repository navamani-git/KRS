namespace KRSDealerManagement.Shared.Extensions
{
    /// <summary>
    /// DateTime extension methods for common operations
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Get first day of current month
        /// </summary>
        public static DateTime FirstDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        /// <summary>
        /// Get last day of current month
        /// </summary>
        public static DateTime LastDayOfMonth(this DateTime date)
        {
            return date.AddMonths(1).AddDays(-1);
        }

        /// <summary>
        /// Get first day of next month
        /// </summary>
        public static DateTime FirstDayOfNextMonth(this DateTime date)
        {
            return date.AddMonths(1).FirstDayOfMonth();
        }

        /// <summary>
        /// Check if date is today
        /// </summary>
        public static bool IsToday(this DateTime date)
        {
            return date.Date == DateTime.Today;
        }

        /// <summary>
        /// Check if date is in the past
        /// </summary>
        public static bool IsPast(this DateTime date)
        {
            return date < DateTime.Now;
        }

        /// <summary>
        /// Check if date is in the future
        /// </summary>
        public static bool IsFuture(this DateTime date)
        {
            return date > DateTime.Now;
        }

        /// <summary>
        /// Get number of days between two dates
        /// </summary>
        public static int DaysBetween(this DateTime startDate, DateTime endDate)
        {
            return Math.Abs((endDate - startDate).Days);
        }

        /// <summary>
        /// Get human-readable time ago format (e.g., "2 hours ago")
        /// </summary>
        public static string TimeAgo(this DateTime date)
        {
            var timeSpan = DateTime.Now.Subtract(date);

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes > 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) > 1 ? "s" : "")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) > 1 ? "s" : "")} ago";
        }

        /// <summary>
        /// Format date to short format (e.g., "07-Aug-2026")
        /// </summary>
        public static string ToShortDateFormat(this DateTime date)
        {
            return date.ToString("dd-MMM-yyyy");
        }

        /// <summary>
        /// Format date to long format with time (e.g., "07 August 2026 14:30:45")
        /// </summary>
        public static string ToLongDateFormat(this DateTime date)
        {
            return date.ToString("dd MMMM yyyy HH:mm:ss");
        }

        /// <summary>
        /// Get start of day (00:00:00)
        /// </summary>
        public static DateTime StartOfDay(this DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// Get end of day (23:59:59)
        /// </summary>
        public static DateTime EndOfDay(this DateTime date)
        {
            return date.Date.AddDays(1).AddSeconds(-1);
        }

        /// <summary>
        /// Convert to ISO 8601 format
        /// </summary>
        public static string ToIso8601(this DateTime date)
        {
            return date.ToString("o");
        }
    }
}

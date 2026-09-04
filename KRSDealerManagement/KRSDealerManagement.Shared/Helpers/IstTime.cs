namespace KRSDealerManagement.Shared.Helpers
{
    public static class IstTime
    {
        private static readonly TimeZoneInfo Zone = ResolveZone();

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        public static DateTime Today => Now.Date;

        public static DateTime? ToIst(DateTime? utcOrUnspecified)
        {
            if (!utcOrUnspecified.HasValue) return null;
            var value = utcOrUnspecified.Value;
            if (value.Kind == DateTimeKind.Utc)
                return TimeZoneInfo.ConvertTimeFromUtc(value, Zone);
            return value;
        }

        public static bool IsFutureDate(DateTime value)
            => value.Date > Today;

        public static bool IsFutureDateTime(DateTime value)
        {
            var ist = value.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(value, Zone)
                : value;
            return ist > Now;
        }

        public static string DateTimeLocalMaxValue()
            => Now.ToString("yyyy-MM-ddTHH:mm");

        public static string DateInputMaxValue()
            => Today.ToString("yyyy-MM-dd");

        private static TimeZoneInfo ResolveZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
                catch { return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "IST", "IST"); }
            }
        }
    }
}

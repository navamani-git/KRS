namespace KRSDealerManagement.Application.Helpers
{
    public static class CommissionRateOverlapHelper
    {
        /// <summary>
        /// True when two inclusive date ranges share more than a boundary day.
        /// Adjacent ranges (e.g. 01–15 and 15–30) are allowed.
        /// </summary>
        public static bool RangesOverlap(DateTime from1, DateTime to1, DateTime from2, DateTime to2)
        {
            var a = from1.Date;
            var b = to1.Date;
            var c = from2.Date;
            var d = to2.Date;
            return a < d && c < b;
        }

        public static string OverlapMessage(DateTime from, DateTime to, DateTime otherFrom, DateTime otherTo)
            => $"This period ({from:yyyy-MM-dd} to {to:yyyy-MM-dd}) overlaps an existing rate "
               + $"({otherFrom:yyyy-MM-dd} to {otherTo:yyyy-MM-dd}) for the same model.";
    }
}

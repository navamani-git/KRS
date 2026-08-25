using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class CommissionRateOverlapHelper
    {
        /// <summary>
        /// True when two inclusive date ranges share at least one day.
        /// Adjacent non-overlapping example: 01–21 then 22–30 (next period starts day after previous ends).
        /// </summary>
        public static bool RangesOverlap(DateTime from1, DateTime to1, DateTime from2, DateTime to2)
        {
            var (a, b) = NormalizeRange(from1, to1);
            var (c, d) = NormalizeRange(from2, to2);
            return a <= d && c <= b;
        }

        public static (DateTime From, DateTime To) NormalizeRange(DateTime from, DateTime to)
            => NormalizeRange(from, to, null, null, null, null);

        public static (DateTime From, DateTime To) NormalizeRange(
            DateTime from,
            DateTime to,
            int? startMonth,
            int? startYear,
            int? expiryMonth,
            int? expiryYear)
        {
            var f = from.Date;
            var t = to.Date;

            if (t.Year < 2000 || t < f)
            {
                if (expiryYear is > 0 && expiryMonth is > 0 and <= 12)
                {
                    t = new DateTime(expiryYear.Value, expiryMonth.Value,
                        DateTime.DaysInMonth(expiryYear.Value, expiryMonth.Value));
                }
                else if (f.Year >= 2000)
                {
                    t = new DateTime(f.Year, f.Month, DateTime.DaysInMonth(f.Year, f.Month));
                }
                else if (startYear is > 0 && startMonth is > 0 and <= 12)
                {
                    f = new DateTime(startYear.Value, startMonth.Value, 1);
                    t = f.AddMonths(1).AddDays(-1);
                }
            }

            if (t < f)
                t = f;

            return (f, t);
        }

        public static (DateTime From, DateTime To) NormalizeRange(CommissionRate rate)
            => NormalizeRange(
                rate.EffectiveFrom,
                rate.EffectiveTo,
                rate.StartMonth,
                rate.StartYear,
                rate.ExpiryMonth,
                rate.ExpiryYear);

        public static string OverlapMessage(DateTime from, DateTime to, DateTime otherFrom, DateTime otherTo)
        {
            var (_, otherEnd) = NormalizeRange(otherFrom, otherTo);
            var nextAllowed = otherEnd.AddDays(1);
            return $"A commission rate is already configured for this model from {otherFrom:yyyy-MM-dd} to {otherEnd:yyyy-MM-dd}. "
                   + $"Edit that record first, or add a new period starting from {nextAllowed:yyyy-MM-dd} or later.";
        }

        public static bool TryFindOverlap(
            IEnumerable<CommissionRate> existing,
            int modelId,
            DateTime from,
            DateTime to,
            int? excludeRateId,
            out CommissionRate? conflict)
        {
            conflict = null;
            var (newFrom, newTo) = NormalizeRange(from, to);

            foreach (var other in existing.Where(r => r.ModelId == modelId && r.CommissionRateId != excludeRateId))
            {
                var (otherFrom, otherTo) = NormalizeRange(other);
                if (RangesOverlap(newFrom, newTo, otherFrom, otherTo))
                {
                    conflict = other;
                    return true;
                }
            }

            return false;
        }
    }
}

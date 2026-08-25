using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class VehiclePriceOverlapHelper
    {
        public static bool RangesOverlap(DateTime from1, DateTime to1, DateTime from2, DateTime to2)
        {
            var (a, b) = NormalizeRange(from1, to1);
            var (c, d) = NormalizeRange(from2, to2);
            return a <= d && c <= b;
        }

        public static (DateTime From, DateTime To) NormalizeRange(DateTime from, DateTime to)
            => NormalizeRange(from, to, null, null);

        public static (DateTime From, DateTime To) NormalizeRange(
            DateTime from,
            DateTime to,
            int? month,
            int? year)
        {
            var f = from.Date;
            var t = to.Date;

            if (t.Year < 2000 || t < f)
            {
                if (month is > 0 and <= 12 && year is > 0)
                {
                    if (f.Year < 2000)
                        f = new DateTime(year.Value, month.Value, 1);
                    t = new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value));
                }
                else if (f.Year >= 2000)
                {
                    t = new DateTime(f.Year, f.Month, DateTime.DaysInMonth(f.Year, f.Month));
                }
            }

            if (t < f)
                t = f;

            return (f, t);
        }

        public static (DateTime From, DateTime To) NormalizeRange(VehiclePriceHistory price)
            => NormalizeRange(price.EffectiveFrom, price.EffectiveTo, price.Month, price.Year);

        public static string OverlapMessage(DateTime from, DateTime to, DateTime otherFrom, DateTime otherTo)
        {
            var (_, otherEnd) = NormalizeRange(otherFrom, otherTo);
            var nextAllowed = otherEnd.AddDays(1);
            return $"A price is already configured for this model and color from {otherFrom:yyyy-MM-dd} to {otherEnd:yyyy-MM-dd}. "
                   + $"Edit that record first, or add a new period starting from {nextAllowed:yyyy-MM-dd} or later.";
        }

        public static bool TryFindOverlap(
            IEnumerable<VehiclePriceHistory> existing,
            int modelId,
            int colorId,
            DateTime from,
            DateTime to,
            int? excludePriceHistoryId,
            out VehiclePriceHistory? conflict)
        {
            conflict = null;
            var (newFrom, newTo) = NormalizeRange(from, to);

            foreach (var other in existing.Where(p =>
                         p.ModelId == modelId
                         && p.ColorId == colorId
                         && p.PriceHistoryId != excludePriceHistoryId))
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

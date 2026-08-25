using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class VehiclePriceCoverageHelper
    {
        public static VehiclePriceHistory? FindActivePrice(
            IEnumerable<VehiclePriceHistory> prices,
            int modelId,
            int colorId,
            DateTime asOfDate)
        {
            var asOf = asOfDate.Date;
            return prices
                .Where(p => p.ModelId == modelId && p.ColorId == colorId)
                .Select(p => (Price: p, Range: VehiclePriceOverlapHelper.NormalizeRange(p)))
                .Where(x => x.Range.From <= asOf && x.Range.To >= asOf)
                .OrderByDescending(x => x.Range.From)
                .ThenByDescending(x => x.Price.PriceHistoryId)
                .Select(x => x.Price)
                .FirstOrDefault();
        }

        public static string? ValidateForDate(
            IEnumerable<VehiclePriceHistory> allPrices,
            int modelId,
            int colorId,
            DateTime asOfDate,
            string? modelLabel = null,
            string? colorLabel = null)
        {
            var asOf = asOfDate.Date;
            var label = FormatLabel(modelLabel, colorLabel, modelId, colorId);

            var relevant = allPrices
                .Where(p => p.ModelId == modelId && p.ColorId == colorId)
                .ToList();

            if (relevant.Count == 0)
                return $"No price configured for {label}.";

            if (FindActivePrice(relevant, modelId, colorId, asOf) != null)
                return null;

            var ranges = relevant
                .Select(p => VehiclePriceOverlapHelper.NormalizeRange(p))
                .OrderBy(r => r.From)
                .ToList();

            var latestBefore = ranges
                .Where(r => r.To < asOf)
                .OrderByDescending(r => r.To)
                .FirstOrDefault();

            if (latestBefore.To != default)
            {
                var nextDay = latestBefore.To.AddDays(1);
                return $"Price for {label} is configured only until {latestBefore.To:yyyy-MM-dd}. "
                       + $"Cannot create a vehicle on {asOf:yyyy-MM-dd}. Add a price from {nextDay:yyyy-MM-dd} or later.";
            }

            var earliestAfter = ranges
                .Where(r => r.From > asOf)
                .OrderBy(r => r.From)
                .FirstOrDefault();

            if (earliestAfter.From != default)
            {
                return $"No price effective on {asOf:yyyy-MM-dd} for {label}. "
                       + $"The next configured period starts on {earliestAfter.From:yyyy-MM-dd}.";
            }

            return $"No price effective on {asOf:yyyy-MM-dd} for {label}.";
        }

        private static string FormatLabel(string? modelLabel, string? colorLabel, int modelId, int colorId)
        {
            if (!string.IsNullOrWhiteSpace(modelLabel) && !string.IsNullOrWhiteSpace(colorLabel))
                return $"{modelLabel} / {colorLabel}";
            return $"model #{modelId} / color #{colorId}";
        }
    }
}

using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class CommissionRateCoverageHelper
    {
        public static CommissionRate? FindActiveRate(
            IEnumerable<CommissionRate> rates,
            int modelId,
            DateTime asOfDate)
        {
            var asOf = asOfDate.Date;
            return rates
                .Where(r => r.ModelId == modelId)
                .Select(r => (Rate: r, Range: CommissionRateOverlapHelper.NormalizeRange(r)))
                .Where(x => x.Range.From <= asOf && x.Range.To >= asOf)
                .OrderByDescending(x => x.Range.From)
                .ThenByDescending(x => x.Rate.CommissionRateId)
                .Select(x => x.Rate)
                .FirstOrDefault();
        }

        public static string ValidateForDate(
            IEnumerable<CommissionRate> allRates,
            int modelId,
            DateTime asOfDate,
            string? modelLabel = null)
        {
            var asOf = asOfDate.Date;
            var label = string.IsNullOrWhiteSpace(modelLabel) ? $"model #{modelId}" : modelLabel;

            var relevant = allRates.Where(r => r.ModelId == modelId).ToList();
            if (relevant.Count == 0)
                return $"No commission rate configured for {label}.";

            if (FindActiveRate(relevant, modelId, asOf) != null)
                return string.Empty;

            var ranges = relevant
                .Select(r => CommissionRateOverlapHelper.NormalizeRange(r))
                .OrderBy(r => r.From)
                .ToList();

            var latestBefore = ranges
                .Where(r => r.To < asOf)
                .OrderByDescending(r => r.To)
                .FirstOrDefault();

            if (latestBefore.To != default)
            {
                var nextDay = latestBefore.To.AddDays(1);
                return $"Commission for {label} is configured only until {latestBefore.To:yyyy-MM-dd}. "
                       + $"Cannot apply commission for {asOf:yyyy-MM-dd}. Add a rate from {nextDay:yyyy-MM-dd} or later.";
            }

            var earliestAfter = ranges
                .Where(r => r.From > asOf)
                .OrderBy(r => r.From)
                .FirstOrDefault();

            if (earliestAfter.From != default)
            {
                return $"No commission rate effective on {asOf:yyyy-MM-dd} for {label}. "
                       + $"The next configured period starts on {earliestAfter.From:yyyy-MM-dd}.";
            }

            return $"No commission rate effective on {asOf:yyyy-MM-dd} for {label}.";
        }
    }
}

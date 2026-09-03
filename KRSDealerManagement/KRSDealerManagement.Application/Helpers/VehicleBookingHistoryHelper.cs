using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Helpers
{
    public static class VehicleBookingHistoryHelper
    {
        public static string? DescribeDateTimeChange(string label, DateTime? before, DateTime? after)
        {
            if (Nullable.Equals(before, after))
                return null;

            return CorrectionNoteHelper.DescribeChange(label, Format(before), Format(after));
        }

        public static string? DescribeTextChange(string label, string? before, string? after)
        {
            var b = before?.Trim();
            var a = after?.Trim();
            if (string.Equals(b, a, StringComparison.OrdinalIgnoreCase))
                return null;

            return CorrectionNoteHelper.DescribeChange(label, b, a);
        }

        public static async Task LogChangesAsync(
            IUnitOfWork unitOfWork,
            int subdealerVehicleId,
            int? userId,
            IEnumerable<string> changes,
            string action = "BookingEdited")
        {
            var notes = changes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (notes.Count == 0)
                return;

            await VehicleHistoryHelper.LogSubdealerEventAsync(
                unitOfWork,
                subdealerVehicleId,
                action,
                userId,
                string.Join("; ", notes));
        }

        private static string Format(DateTime? value)
            => CorrectionNoteLabelResolver.DateTimeValue(value);
    }
}

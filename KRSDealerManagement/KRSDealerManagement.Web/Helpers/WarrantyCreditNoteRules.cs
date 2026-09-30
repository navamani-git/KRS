using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Web.Helpers
{
    public static class WarrantyCreditNoteRules
    {
        public const string ResolutionCode = "CREDIT_NOTE";
        public const decimal HandlingPercentOfUnitPrice = 5m;

        /// <summary>Returns an error message, or null with <paramref name="creditNote"/> populated.</summary>
        public static string? TryBuild(
            int warrantyClaimId,
            decimal? unitPrice, int? unitGstRateId,
            decimal? handlingCharges, int? handlingGstRateId,
            decimal? labourCharges, int? labourGstRateId,
            IReadOnlyDictionary<int, decimal> activeRatePercents,
            out WarrantyCreditNote? creditNote)
        {
            creditNote = null;

            if (!unitPrice.HasValue || unitPrice.Value <= 0)
                return "Credit Note: Unit price is required and must be greater than 0.";
            if (!handlingCharges.HasValue || handlingCharges.Value < 0)
                return "Credit Note: Handling charges are required.";
            if (!labourCharges.HasValue || labourCharges.Value < 0)
                return "Credit Note: Labour charges are required (0 allowed).";

            if (!unitGstRateId.HasValue || !activeRatePercents.TryGetValue(unitGstRateId.Value, out var unitPct))
                return "Credit Note: Select GST for unit price.";
            if (!handlingGstRateId.HasValue || !activeRatePercents.TryGetValue(handlingGstRateId.Value, out var handlingPct))
                return "Credit Note: Select GST for handling charges.";
            if (!labourGstRateId.HasValue || !activeRatePercents.TryGetValue(labourGstRateId.Value, out var labourPct))
                return "Credit Note: Select GST for labour charges.";

            var unit = Math.Round(unitPrice.Value, 2);
            var handling = Math.Round(handlingCharges.Value, 2);
            var labour = Math.Round(labourCharges.Value, 2);

            creditNote = new WarrantyCreditNote
            {
                WarrantyClaimId = warrantyClaimId,
                UnitPrice = unit,
                UnitPriceGstRateId = unitGstRateId.Value,
                UnitPriceGstPercent = unitPct,
                HandlingCharges = handling,
                HandlingGstRateId = handlingGstRateId.Value,
                HandlingGstPercent = handlingPct,
                LabourCharges = labour,
                LabourGstRateId = labourGstRateId.Value,
                LabourGstPercent = labourPct,
                TotalWithoutGst = unit + handling + labour,
                TotalWithGst = Math.Round(WithGst(unit, unitPct) + WithGst(handling, handlingPct) + WithGst(labour, labourPct), 2)
            };
            return null;
        }

        private static decimal WithGst(decimal amount, decimal percent) => amount + amount * percent / 100m;
    }
}

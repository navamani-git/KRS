using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    public static class VehicleAgingCalculator
    {
        public static int? CalendarDays(DateTime? start, DateTime? endOrToday)
        {
            if (!start.HasValue)
                return null;

            var from = ToAgingDate(start.Value);
            var to = ToAgingDate(endOrToday ?? IstTime.Now);
            return Math.Max(0, (to - from).Days);
        }

        /// <summary>
        /// Milestone ageing for the ageing grid: 0 when the step is done,
        /// days since the previous step when still pending, null when the previous step is not reached yet.
        /// </summary>
        public static int? StageAging(DateTime? previousStageDate, DateTime? currentStageDate)
        {
            if (!previousStageDate.HasValue)
                return null;

            if (currentStageDate.HasValue)
                return 0;

            return CalendarDays(previousStageDate, null);
        }

        /// <summary>
        /// Computes all ageing columns from milestone dates.
        /// Chain matches Manage booking milestones:
        /// Allocate → Booked → Paper → Invoice → Insurance → Agent (RTO requested) → Registration (RTO number) → Subsidy ID → Subsidy docs approved.
        /// </summary>
        public static VehicleAgingResults Compute(VehicleAgingInputs input)
        {
            var subsidyCompleteDate = input.HasSubsidyId
                ? input.SubsidyIdDate ?? input.RegistrationDate
                : null;

            return new VehicleAgingResults(
                BookedAging: StageAging(input.PurchaseDate, input.BookedDate),
                PaperReceivedAging: StageAging(input.BookedDate, input.PaperReceivedDate),
                InvoiceAging: StageAging(input.PaperReceivedDate, input.InvoiceDate),
                InsuranceAging: StageAging(input.InvoiceDate, input.InsuranceDate),
                AgentAging: StageAging(input.InsuranceDate, input.AgentDate),
                RegistrationAging: StageAging(input.AgentDate, input.RegistrationDate),
                SubsidyAging: StageAging(input.RegistrationDate, subsidyCompleteDate),
                SubsidyDocumentsAging: input.HasSubsidyId
                    ? StageAging(input.SubsidyIdDate, input.SubsidyCompletedApprovedDate)
                    : null);
        }

        public static int PendingAgingScore(VehicleAgingResults aging) =>
            new[]
            {
                aging.BookedAging,
                aging.PaperReceivedAging,
                aging.InvoiceAging,
                aging.InsuranceAging,
                aging.AgentAging,
                aging.RegistrationAging,
                aging.SubsidyAging,
                aging.SubsidyDocumentsAging
            }.Where(v => v.HasValue).DefaultIfEmpty(0).Max(v => v!.Value);

        private static DateTime ToAgingDate(DateTime value)
        {
            var ist = IstTime.ToIst(value) ?? value;
            return ist.Date;
        }

        public static bool ShouldAppearOnAgingScreen(
            int vehicleStatus,
            int? subdealerId,
            bool subsidyCompletedApproved)
        {
            if (!subdealerId.HasValue || subdealerId.Value <= 0)
                return false;
            if (subsidyCompletedApproved)
                return false;
            if (vehicleStatus is UnifiedVehicleStatus.ReturnRequested or UnifiedVehicleStatus.ReturnApproved)
                return false;
            if (vehicleStatus == UnifiedVehicleStatus.RejectedByDealer)
                return false;
            return true;
        }
    }

    public readonly record struct VehicleAgingInputs(
        DateTime? PurchaseDate,
        DateTime? BookedDate,
        DateTime? PaperReceivedDate,
        DateTime? InvoiceDate,
        DateTime? InsuranceDate,
        DateTime? AgentDate,
        DateTime? RegistrationDate,
        bool HasSubsidyId,
        DateTime? SubsidyIdDate,
        DateTime? SubsidyCompletedApprovedDate);

    public readonly record struct VehicleAgingResults(
        int? BookedAging,
        int? PaperReceivedAging,
        int? InvoiceAging,
        int? InsuranceAging,
        int? AgentAging,
        int? RegistrationAging,
        int? SubsidyAging,
        int? SubsidyDocumentsAging);
}

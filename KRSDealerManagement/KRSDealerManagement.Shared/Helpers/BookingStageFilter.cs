using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    /// <summary>
    /// Maps vehicles to booking pipeline stage screens using vehicle status and booking milestone dates.
    /// </summary>
    public static class BookingStageFilter
    {
        public static int ResolveFromMilestones(
            DateTime? paperReceivedDate,
            DateTime? invoiceDate,
            DateTime? insuranceDate,
            DateTime? agentDate,
            DateTime? registrationDate,
            string? subsidyId)
        {
            if (!string.IsNullOrWhiteSpace(subsidyId))
                return UnifiedVehicleStatus.SubsidyIdCreated;
            if (registrationDate.HasValue)
                return UnifiedVehicleStatus.Registered;
            if (agentDate.HasValue)
                return UnifiedVehicleStatus.RtoRequested;
            if (insuranceDate.HasValue)
                return UnifiedVehicleStatus.InsuranceCreated;
            if (invoiceDate.HasValue)
                return UnifiedVehicleStatus.Invoiced;
            if (paperReceivedDate.HasValue)
                return UnifiedVehicleStatus.PaperReceived;
            return UnifiedVehicleStatus.BookedToCustomer;
        }

        public static int ResolveEffectiveStage(
            int vehicleStatus,
            DateTime? paperReceivedDate,
            DateTime? invoiceDate,
            DateTime? insuranceDate,
            DateTime? agentDate,
            DateTime? registrationDate,
            string? subsidyId)
        {
            if (vehicleStatus == UnifiedVehicleStatus.Delivered)
                return UnifiedVehicleStatus.Delivered;

            if (vehicleStatus < UnifiedVehicleStatus.BookedToCustomer)
                return vehicleStatus;

            var fromMilestones = ResolveFromMilestones(
                paperReceivedDate, invoiceDate, insuranceDate, agentDate, registrationDate, subsidyId);
            return Math.Max(vehicleStatus, fromMilestones);
        }

        public static bool MatchesStage(int vehicleStatus, int stageStatus) => stageStatus switch
        {
            UnifiedVehicleStatus.Registered => vehicleStatus
                is UnifiedVehicleStatus.Registered or UnifiedVehicleStatus.SubsidyIdCreated,
            _ => vehicleStatus == stageStatus
        };

        public static bool MatchesStage(
            int vehicleStatus,
            int stageStatus,
            DateTime? paperReceivedDate,
            DateTime? invoiceDate,
            DateTime? insuranceDate,
            DateTime? agentDate,
            DateTime? registrationDate,
            string? subsidyId)
        {
            var effective = ResolveEffectiveStage(
                vehicleStatus,
                paperReceivedDate,
                invoiceDate,
                insuranceDate,
                agentDate,
                registrationDate,
                subsidyId);
            return MatchesStage(effective, stageStatus);
        }

        public static bool IsBookingPhase(int vehicleStatus) =>
            UnifiedVehicleStatus.IsBookingPhase(vehicleStatus);

        /// <summary>
        /// Returns an error message when status is behind milestone dates or ahead of available dates.
        /// </summary>
        public static string? ValidateBookingStatusSelection(
            int bookingStatus,
            DateTime? paperReceivedDate,
            DateTime? invoiceDate,
            DateTime? insuranceDate,
            DateTime? agentDate,
            DateTime? registrationDate,
            string? subsidyId)
        {
            if (bookingStatus < UnifiedVehicleStatus.BookedToCustomer
                || bookingStatus > UnifiedVehicleStatus.SubsidyIdCreated)
                return null;

            var minFromDates = ResolveFromMilestones(
                paperReceivedDate, invoiceDate, insuranceDate, agentDate, registrationDate, subsidyId);

            if (bookingStatus < minFromDates)
                return "Status cannot be earlier than the latest milestone date entered. Clear later dates first or choose a matching status.";

            if (bookingStatus >= UnifiedVehicleStatus.PaperReceived && !paperReceivedDate.HasValue)
                return "Paper Received date is required for the selected status.";
            if (bookingStatus >= UnifiedVehicleStatus.Invoiced && !invoiceDate.HasValue)
                return "Invoice date is required for the selected status.";
            if (bookingStatus >= UnifiedVehicleStatus.InsuranceCreated && !insuranceDate.HasValue)
                return "Insurance date is required for the selected status.";
            if (bookingStatus >= UnifiedVehicleStatus.RtoRequested && !agentDate.HasValue)
                return "Agent date is required for the selected status.";
            if (bookingStatus >= UnifiedVehicleStatus.Registered && !registrationDate.HasValue)
                return "Registration date is required for the selected status.";
            if (bookingStatus >= UnifiedVehicleStatus.SubsidyIdCreated && string.IsNullOrWhiteSpace(subsidyId))
                return "Subsidy ID is required for the selected status.";

            return null;
        }

        public static bool IsStatusSelectable(
            int statusValue,
            DateTime? paperReceivedDate,
            DateTime? invoiceDate,
            DateTime? insuranceDate,
            DateTime? agentDate,
            DateTime? registrationDate,
            string? subsidyId) =>
            ValidateBookingStatusSelection(
                statusValue,
                paperReceivedDate,
                invoiceDate,
                insuranceDate,
                agentDate,
                registrationDate,
                subsidyId) == null;
    }
}

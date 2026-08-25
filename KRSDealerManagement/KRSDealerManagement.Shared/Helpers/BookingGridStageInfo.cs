using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    /// <summary>
    /// User-facing descriptions for booking grid screens: which statuses appear and when a row leaves the list.
    /// </summary>
    public static class BookingGridStageInfo
    {
        public readonly record struct GridStageDescription(string Showing, string RemovedWhen);

        public static GridStageDescription Describe(int? status, bool bookingPhaseOnly, bool bookedToCustomerView = false)
        {
            if (bookedToCustomerView || status == UnifiedVehicleStatus.BookedToCustomer)
            {
                return new GridStageDescription(
                    "Booked to Customer only",
                    "Paper Received date is saved on Manage");
            }

            if (bookingPhaseOnly)
            {
                return new GridStageDescription(
                    "Booked to Customer through Delivered",
                    "vehicle is no longer in the booking pipeline");
            }

            if (!status.HasValue)
            {
                return new GridStageDescription(
                    "All booking statuses",
                    "a stage filter is applied");
            }

            return status.Value switch
            {
                UnifiedVehicleStatus.PaperReceived => new(
                    "Paper Received only",
                    "Invoice date is saved on Manage"),
                UnifiedVehicleStatus.Invoiced => new(
                    "Invoiced only",
                    "Insurance date is saved on Manage"),
                UnifiedVehicleStatus.InsuranceCreated => new(
                    "Insurance Created only",
                    "Agent date is saved on Manage"),
                UnifiedVehicleStatus.RtoRequested => new(
                    "RTO Requested only",
                    "Registration date is saved on Manage"),
                UnifiedVehicleStatus.Registered => new(
                    "Registered and Subsidy ID Created",
                    "subdealer marks the vehicle as Delivered"),
                UnifiedVehicleStatus.Delivered => new(
                    "Delivered only",
                    "—"),
                _ => new GridStageDescription("Selected booking status", "status advances to the next stage")
            };
        }

        public static string FormatForHeader(int? status, bool bookingPhaseOnly, bool bookedToCustomerView = false)
        {
            var info = Describe(status, bookingPhaseOnly, bookedToCustomerView);
            return $"Showing: {info.Showing} · Removed when: {info.RemovedWhen}";
        }
    }
}

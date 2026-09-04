using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    /// <summary>
    /// Plain-language descriptions for booking dashboard pills and list screens.
    /// </summary>
    public static class BookingGridStageInfo
    {
        public readonly record struct GridStageDescription(string Showing, string RemovedWhen);

        public readonly record struct StageHelp(
            string Showing,
            string RemovedWhen,
            string PillSummary,
            string ScreenPurpose);

        public static StageHelp GetHelp(
            int? status = null,
            bool bookingPhaseOnly = false,
            bool bookedToCustomerView = false,
            bool subsidyIdPendingOnly = false,
            bool subsidyDocsPendingOnly = false,
            bool registeredAwaitingPlateOnly = false)
        {
            if (subsidyDocsPendingOnly)
                return SubsidyDocsPending();

            if (registeredAwaitingPlateOnly)
                return RegisteredAwaitingPlate();

            if (subsidyIdPendingOnly)
                return SubsidyIdPending();

            if (bookedToCustomerView || status == UnifiedVehicleStatus.BookedToCustomer)
                return BookedToCustomer();

            if (bookingPhaseOnly)
            {
                return new StageHelp(
                    "All active booking stages (Booked to Customer through Delivered)",
                    "the vehicle is marked Delivered or leaves the booking process",
                    "All vehicles currently moving through the booking steps",
                    "This workspace is for staff to process bookings. Use the stage-specific menus to view one step at a time.");
            }

            if (!status.HasValue)
            {
                return new StageHelp(
                    "All booking stages",
                    "you pick a stage filter from the menu or dashboard",
                    "All vehicles in the booking process",
                    "This list shows every vehicle in the booking pipeline. Use dashboard pills or the status filter to focus on one step.");
            }

            return status.Value switch
            {
                UnifiedVehicleStatus.PaperReceived => PaperReceived(),
                UnifiedVehicleStatus.Invoiced => Invoiced(),
                UnifiedVehicleStatus.InsuranceCreated => InsuranceCreated(),
                UnifiedVehicleStatus.RtoRequested => RtoRequested(),
                UnifiedVehicleStatus.Registered => Registered(),
                UnifiedVehicleStatus.Delivered => new StageHelp(
                    "Delivered to customer only",
                    "—",
                    "Handed over to the customer",
                    "Vehicles that have been marked as delivered to the customer."),
                _ => new StageHelp(
                    "Selected booking stage",
                    "the vehicle moves to the next stage on Manage",
                    "Vehicles at the selected booking step",
                    "This list shows vehicles that match the selected booking stage.")
            };
        }

        public static GridStageDescription Describe(int? status, bool bookingPhaseOnly, bool bookedToCustomerView = false)
        {
            var help = GetHelp(status, bookingPhaseOnly, bookedToCustomerView);
            return new GridStageDescription(help.Showing, help.RemovedWhen);
        }

        public static GridStageDescription Describe(
            int? status,
            bool bookingPhaseOnly,
            bool bookedToCustomerView,
            bool subsidyIdPendingOnly)
        {
            var help = GetHelp(status, bookingPhaseOnly, bookedToCustomerView, subsidyIdPendingOnly);
            return new GridStageDescription(help.Showing, help.RemovedWhen);
        }

        public static string FormatForHeader(int? status, bool bookingPhaseOnly, bool bookedToCustomerView = false)
        {
            var info = Describe(status, bookingPhaseOnly, bookedToCustomerView);
            return $"Showing: {info.Showing} · Removed when: {info.RemovedWhen}";
        }

        private static StageHelp BookedToCustomer() => new(
            "Customer booking is saved; paper work has not been received yet",
            "Paper Received date is saved on the Manage screen",
            "Booked for a customer — waiting for Rto papers",
            "Shows vehicles where the customer booking is complete but the required papers have not yet been received. Open Manage and save the Paper Received date when papers arrive.");

        private static StageHelp PaperReceived() => new(
            "Paper work is received; invoice has not been created yet",
            "Invoice date is saved on the Manage screen",
            "Papers received — waiting for invoice",
            "Shows vehicles whose documents are received but the invoice step is not done yet. Save the Invoice date on Manage after invoicing.");

        private static StageHelp Invoiced() => new(
            "Vehicle is invoiced; insurance is not recorded yet",
            "Insurance date is saved on the Manage screen",
            "Invoiced — waiting for insurance",
            "Shows vehicles that are invoiced but insurance details are still pending. Save the Insurance date on Manage once insurance is created.");

        private static StageHelp InsuranceCreated() => new(
            "Insurance is done; RTO agent step is not recorded yet",
            "Agent (RTO) date is saved on the Manage screen",
            "Insurance done — waiting for RTO agent",
            "Shows vehicles with insurance completed but the vehicle is not yet in agent hand. Save the Agent date on Manage when the RTO agent takes the vehicle.");

        private static StageHelp RtoRequested() => new(
            "In agent hand, waiting for registration",
            "Registration date is saved on the Manage screen",
            "In agent hand — waiting for registration",
            "Shows vehicles handed to the RTO agent but registration is not saved yet. Save the Registration date and RTO number on Manage when registration is complete.");

        private static StageHelp Registered() => new(
            "Vehicle is registered (includes vehicles with subsidy ID entered)",
            "the subdealer marks the vehicle as Delivered on Manage",
            "Registered — waiting for delivery to customer",
            "Shows registered vehicles, including those that already have a subsidy ID. A vehicle leaves this list when it is marked Delivered.");

        public static StageHelp RegisteredAwaitingPlate() => new(
            "Vehicle is registered at RTO but number plate has not been received yet",
            "the subdealer saves number plate received date and received-by name",
            "Registered — waiting for number plate",
            "Shows registered vehicles whose RTO number is recorded. Subdealers mark when the number plate is received; the vehicle then leaves this list while staying Registered until delivery.");

        public static StageHelp SubsidyDocsPending() => new(
            "Subsidy ID is assigned but one or more subsidy documents are still missing",
            "all four subsidy documents are uploaded (face verification, RC image, booth photo, undertaking)",
            "Subsidy ID set — documents pending",
            "Shows vehicles with a subsidy ID where face verification, RC image, booth photo, or undertaking is still missing. Subdealers upload documents one at a time from the upload screen.");

        public static StageHelp SubsidyIdPending() => new(
            "Invoice date and insurance date are saved, but subsidy ID is still empty",
            "Subsidy ID is saved on the Manage screen",
            "Invoice & insurance done — subsidy ID missing",
            "Shows vehicles where billing and insurance are complete but the subsidy ID has not been entered yet. Open Manage and enter the Subsidy ID when you receive it.");
    }
}

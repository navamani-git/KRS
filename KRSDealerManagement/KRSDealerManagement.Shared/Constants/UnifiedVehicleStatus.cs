namespace KRSDealerManagement.Shared.Constants
{
    /// <summary>
    /// Single lifecycle status for Vehicles (PO, booking, return all use this).
    /// Stored in Vehicles.VehicleStatus / entity Status.
    /// </summary>
    public static class UnifiedVehicleStatus
    {
        public const int Submitted = 1;
        public const int ApprovedByDealer = 2;
        public const int RejectedByDealer = 3;
        public const int ReturnRequested = 4;
        public const int ReturnApproved = 5;
        public const int ReturnCancelled = 6;
        public const int BookedToCustomer = 7;
        public const int PaperReceived = 8;
        public const int Invoiced = 9;
        public const int InsuranceCreated = 10;
        public const int RtoRequested = 11;
        public const int Registered = 12;
        public const int SubsidyIdCreated = 13;
        public const int Delivered = 14;

        public static bool IsTerminal(int status) =>
            status is RejectedByDealer or ReturnApproved or Delivered;

        /// <summary>
        /// Subdealer may return an allocated vehicle before customer booking and before invoice.
        /// Delivered vehicles cannot be returned.
        /// </summary>
        public static bool CanRequestReturn(int status) =>
            status is ApprovedByDealer;

        /// <summary>
        /// Subdealer may start customer booking before invoice.
        /// Includes vehicles mistakenly marked Delivered before booking.
        /// </summary>
        public static bool CanStartBooking(int status) =>
            status is ApprovedByDealer or ReturnCancelled or Delivered;

        /// <summary>
        /// True when subdealer book/return actions are still allowed (no booking yet, not invoiced).
        /// </summary>
        public static bool CanBookOrReturnPreInvoice(int status, bool hasBooking, DateTime? invoiceDate) =>
            !hasBooking
            && !invoiceDate.HasValue
            && CanStartBooking(status);

        public static bool IsBookingPhase(int status) => status >= BookedToCustomer && status <= Delivered;

        public static bool IsReturnPhase(int status) =>
            status is ReturnRequested or ReturnApproved or ReturnCancelled;

        public static string PlaceholderChassis(int orderId, int orderItemId) =>
            $"PENDING-{orderId:D5}-{orderItemId:D5}";

        public static bool IsPlaceholderChassis(string? chassis) =>
            !string.IsNullOrWhiteSpace(chassis)
            && chassis.StartsWith("PENDING-", StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyList<int> BookingAssignableStatuses() =>
            new[] { PaperReceived, Invoiced, InsuranceCreated, RtoRequested, Registered, SubsidyIdCreated };

        public static bool IsStaffAssignable(int status) => status != Delivered;
    }
}

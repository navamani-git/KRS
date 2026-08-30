using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    /// <summary>
    /// A vehicle is showroom stock when allocated to a subdealer, not booked, and not invoiced.
    /// </summary>
    public static class ShowroomStockFilter
    {
        public static bool IsShowroomStock(int vehicleStatus, int? subdealerId, DateTime? bookingInvoiceDate, bool hasBooking)
        {
            if (!subdealerId.HasValue || subdealerId.Value <= 0)
                return false;

            if (vehicleStatus is UnifiedVehicleStatus.RejectedByDealer
                or UnifiedVehicleStatus.ReturnRequested
                or UnifiedVehicleStatus.ReturnApproved
                or UnifiedVehicleStatus.Delivered
                or UnifiedVehicleStatus.Submitted)
                return false;

            if (bookingInvoiceDate.HasValue)
                return false;

            if (hasBooking)
                return false;

            return vehicleStatus == UnifiedVehicleStatus.ApprovedByDealer;
        }
    }
}

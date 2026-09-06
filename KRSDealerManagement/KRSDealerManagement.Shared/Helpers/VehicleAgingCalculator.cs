using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Shared.Helpers
{
    public static class VehicleAgingCalculator
    {
        public static int? CalendarDays(DateTime? start, DateTime? endOrToday)
        {
            if (!start.HasValue)
                return null;

            var from = IstTime.ToIst(start.Value)!.Value.Date;
            var to = IstTime.ToIst(endOrToday ?? IstTime.Now)!.Value.Date;
            return Math.Max(0, (to - from).Days);
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
}

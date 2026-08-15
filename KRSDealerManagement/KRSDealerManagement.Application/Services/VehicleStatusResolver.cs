using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Services
{
    public static class VehicleStatusResolver
    {
        public static int ResolveOrderDisplayStatus(IEnumerable<Vehicle> vehicles, IEnumerable<PurchaseOrderItem> items)
        {
            var vehicleList = vehicles.ToList();
            if (vehicleList.Count == 0)
            {
                var pending = items.Count(i => i.IsPending());
                if (pending > 0) return UnifiedVehicleStatus.Submitted;
                return UnifiedVehicleStatus.RejectedByDealer;
            }

            if (vehicleList.All(v => v.Status == UnifiedVehicleStatus.RejectedByDealer))
                return UnifiedVehicleStatus.RejectedByDealer;

            if (vehicleList.Any(v => v.Status == UnifiedVehicleStatus.Submitted))
                return UnifiedVehicleStatus.Submitted;

            return vehicleList.Min(v => v.Status);
        }

        public static int ResolveReturnDisplayStatus(ReturnRequest returnRequest, Vehicle? _)
        {
            return returnRequest.Status switch
            {
                0 => UnifiedVehicleStatus.ReturnRequested,
                1 => UnifiedVehicleStatus.ReturnApproved,
                2 => UnifiedVehicleStatus.ReturnCancelled,
                _ => UnifiedVehicleStatus.ReturnRequested
            };
        }

        public static bool OrderHasPendingAllocation(IEnumerable<PurchaseOrderItem> items, IEnumerable<Vehicle> vehicles)
        {
            var vehicleIds = vehicles.Select(v => v.VehicleId).ToHashSet();
            return items.Any(i => i.VehicleId.HasValue && vehicleIds.Contains(i.VehicleId.Value)
                && vehicles.First(v => v.VehicleId == i.VehicleId).Status == UnifiedVehicleStatus.Submitted);
        }
    }
}

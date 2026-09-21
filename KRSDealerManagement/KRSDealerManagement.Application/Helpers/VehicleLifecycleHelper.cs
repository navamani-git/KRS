using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Helpers
{
    /// <summary>
    /// One chassis (VehicleMaster) may have many SubdealerVehicles rows over time for audit,
    /// but only one non-superseded row is operational at a time.
    /// </summary>
    public static class VehicleLifecycleHelper
    {
        public static bool IsSuperseded(int status)
            => status == UnifiedVehicleStatus.LifecycleSuperseded;

        public static bool IsActiveLifecycle(int status)
            => !IsSuperseded(status);

        /// <summary>
        /// True when a SubdealerVehicles row is currently held by a subdealer org.
        /// Returned dealer-stock rows (SubdealerId cleared) are excluded from Subdealer Vehicles lists.
        /// </summary>
        public static bool IsHeldBySubdealer(Vehicle vehicle)
            => vehicle.SubdealerId is > 0;

        /// <summary>
        /// Returned (or legacy) dealer-showroom stock: no subdealer holder, ready for re-allocation from Dealer Stock.
        /// </summary>
        public static bool IsAtDealerStock(Vehicle vehicle)
            => !IsHeldBySubdealer(vehicle)
               && vehicle.Status is UnifiedVehicleStatus.ReturnApproved
                   or UnifiedVehicleStatus.ApprovedByDealer;

        public static IEnumerable<Vehicle> FilterActiveLifecycle(IEnumerable<Vehicle> vehicles)
            => vehicles.Where(v => IsActiveLifecycle(v.Status));

        public static IEnumerable<Vehicle> GetActiveRowsForMaster(IEnumerable<Vehicle> vehicles, int vehicleMasterId)
            => FilterActiveLifecycle(vehicles).Where(v => v.VehicleMasterId == vehicleMasterId);

        public static Vehicle? GetActiveRowForMaster(IEnumerable<Vehicle> vehicles, int vehicleMasterId)
            => GetActiveRowsForMaster(vehicles, vehicleMasterId)
                .OrderByDescending(v => v.ModifiedDate)
                .ThenByDescending(v => v.VehicleId)
                .FirstOrDefault();

        public static async Task EnsureCanAllocateToSubdealerAsync(
            IUnitOfWork unitOfWork,
            VehicleMaster master,
            int targetSubDealerOrgId)
        {
            var activeRows = GetActiveRowsForMaster(await unitOfWork.Vehicles.GetAllAsync(), master.VehicleMasterId).ToList();
            foreach (var existing in activeRows)
            {
                if (!existing.SubdealerId.HasValue)
                    continue;

                if (existing.SubdealerId.Value != targetSubDealerOrgId)
                {
                    throw new InvalidOperationException(
                        $"Chassis {master.ChassisNumber} is already active with another subdealer. " +
                        "Complete or return that allocation before assigning to a different subdealer.");
                }

                if (existing.Status == UnifiedVehicleStatus.ApprovedByDealer
                    || existing.Status == UnifiedVehicleStatus.ReturnRequested)
                {
                    throw new InvalidOperationException(
                        $"Chassis {master.ChassisNumber} is already allocated to this subdealer.");
                }
            }
        }

        public static async Task SupersedeActiveRowsForMasterAsync(
            IUnitOfWork unitOfWork,
            int vehicleMasterId,
            int? userId,
            string? remarks = null)
        {
            var activeRows = GetActiveRowsForMaster(await unitOfWork.Vehicles.GetAllAsync(), vehicleMasterId).ToList();
            if (activeRows.Count == 0)
                return;

            var note = string.IsNullOrWhiteSpace(remarks)
                ? "Superseded — replaced by a new allocation cycle."
                : remarks.Trim();

            foreach (var row in activeRows)
            {
                row.Status = UnifiedVehicleStatus.LifecycleSuperseded;
                row.ModifiedDate = DateTime.UtcNow;
                row.ModifiedBy = userId;
                await unitOfWork.Vehicles.UpdateAsync(row);
                await VehicleAllocationHelper.LogSubdealerEventAsync(
                    unitOfWork,
                    row.VehicleId,
                    "LifecycleSuperseded",
                    userId,
                    note);
            }
        }
    }
}

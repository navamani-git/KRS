using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Helpers
{
    public static class VehicleAllocationHelper
    {
        public static async Task<int> AllocateFromMasterAsync(
            IUnitOfWork unitOfWork,
            int vehicleMasterId,
            PurchaseOrderItem item,
            int orderId,
            int subdealerId,
            int allocatedBy,
            int status,
            decimal price,
            string? remarks = null)
        {
            var master = await unitOfWork.VehicleMasters.GetByIdAsync(vehicleMasterId)
                ?? throw new InvalidOperationException("Selected chassis was not found in dealer stock.");

            if (master.IsAllocated)
                throw new InvalidOperationException($"Chassis {master.ChassisNumber} is already allocated.");

            if (master.ModelId != item.ModelId || master.ColorId != item.ColorId)
                throw new InvalidOperationException("Selected chassis does not match the order line model and color.");

            var vehicle = new Vehicle
            {
                VehicleMasterId = master.VehicleMasterId,
                ModelId = master.ModelId,
                ColorId = master.ColorId,
                ChassisNumber = master.ChassisNumber,
                Status = status,
                PurchaseOrderId = orderId,
                SubdealerId = subdealerId,
                CurrentPrice = price,
                OriginalPrice = price,
                MotorNo = master.MotorNo,
                BatteryNo = master.BatteryNo,
                ChargerNo = master.ChargerNo,
                ControllerNo = master.ControllerNo,
                ConverterNo = master.ConverterNo,
                ManufacturingYear = 0,
                CreatedBy = allocatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var subdealerVehicleId = await unitOfWork.Vehicles.AddAsync(vehicle);
            await unitOfWork.VehicleMasters.SetAllocatedAsync(master.VehicleMasterId, true, allocatedBy);
            await unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = master.VehicleMasterId,
                Action = "Allocated",
                Remarks = remarks,
                UserId = allocatedBy
            });
            await unitOfWork.SubdealerVehicleHistories.AddAsync(new SubdealerVehicleHistory
            {
                SubdealerVehicleId = subdealerVehicleId,
                Action = "Allocated",
                Remarks = remarks,
                UserId = allocatedBy
            });

            return subdealerVehicleId;
        }

        public static async Task ReleaseMasterAsync(IUnitOfWork unitOfWork, int vehicleMasterId, int? userId, string? remarks = null)
        {
            await unitOfWork.VehicleMasters.SetAllocatedAsync(vehicleMasterId, false, userId);
            await unitOfWork.VehicleMasters.AddHistoryAsync(new VehicleMasterHistory
            {
                VehicleMasterId = vehicleMasterId,
                Action = "Returned",
                Remarks = remarks,
                UserId = userId
            });
        }

        public static async Task LogSubdealerEventAsync(
            IUnitOfWork unitOfWork,
            int subdealerVehicleId,
            string action,
            int? userId,
            string? remarks = null)
        {
            await unitOfWork.SubdealerVehicleHistories.AddAsync(new SubdealerVehicleHistory
            {
                SubdealerVehicleId = subdealerVehicleId,
                Action = action,
                Remarks = remarks,
                UserId = userId
            });
        }
    }
}

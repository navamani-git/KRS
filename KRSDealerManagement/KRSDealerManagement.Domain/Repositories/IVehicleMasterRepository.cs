using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IVehicleMasterRepository : IRepository<VehicleMaster>
    {
        Task<IEnumerable<VehicleMaster>> GetAvailableByModelColorAsync(int dealershipId, int modelId, int colorId);
        Task<VehicleMaster?> GetByChassisAsync(string chassisNumber);
        Task<bool> ChassisExistsAsync(string chassisNumber, int? excludeVehicleMasterId = null);
        Task SetAllocatedAsync(int vehicleMasterId, bool isAllocated, int? modifiedBy);
        Task AddHistoryAsync(VehicleMasterHistory history);
        Task<IEnumerable<VehicleMasterHistory>> GetHistoryAsync(int vehicleMasterId);
    }
}

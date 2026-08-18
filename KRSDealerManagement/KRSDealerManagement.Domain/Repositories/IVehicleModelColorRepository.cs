using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IVehicleModelColorRepository
    {
        Task<IEnumerable<int>> GetColorIdsByModelIdAsync(int modelId);
        Task<bool> IsMappedAsync(int modelId, int colorId);
        Task SyncMappingsAsync(int modelId, IReadOnlyList<int> colorIds, int userId);
        Task<IEnumerable<VehicleModelColor>> GetAllAsync();
    }
}

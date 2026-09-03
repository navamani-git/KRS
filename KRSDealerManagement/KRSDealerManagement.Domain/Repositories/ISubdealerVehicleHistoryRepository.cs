using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface ISubdealerVehicleHistoryRepository
    {
        Task AddAsync(SubdealerVehicleHistory history);
        Task DeleteBySubdealerVehicleIdAsync(int subdealerVehicleId);
        Task<IEnumerable<SubdealerVehicleHistory>> GetBySubdealerVehicleIdAsync(int subdealerVehicleId);
    }
}

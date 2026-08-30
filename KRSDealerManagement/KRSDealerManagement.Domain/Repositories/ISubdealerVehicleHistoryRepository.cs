using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface ISubdealerVehicleHistoryRepository
    {
        Task AddAsync(SubdealerVehicleHistory history);
        Task<IEnumerable<SubdealerVehicleHistory>> GetBySubdealerVehicleIdAsync(int subdealerVehicleId);
    }
}

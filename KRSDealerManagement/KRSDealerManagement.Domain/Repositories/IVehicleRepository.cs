using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IVehicleRepository : IRepository<Vehicle>
    {
        Task<IEnumerable<Vehicle>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    }
}

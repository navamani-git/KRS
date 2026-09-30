using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IGstScreenDefaultRepository : IRepository<GstScreenDefault>
    {
        Task<GstScreenDefault?> GetByScreenKeyAsync(string screenKey);
        Task<IDictionary<string, int>> GetRateIdByScreenKeyAsync();
        Task UpsertAsync(string screenKey, int gstRateId);
    }
}

using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task<IEnumerable<AuditLog>> GetRecentAsync(DateTime sinceUtc, int take, IReadOnlyCollection<int>? userIds = null);
    }
}

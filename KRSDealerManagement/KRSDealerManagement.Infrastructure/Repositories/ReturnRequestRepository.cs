using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class ReturnRequestRepository : Repository<ReturnRequest>
    {
        public ReturnRequestRepository(ApplicationDbContext context)
            : base(context, "ReturnRequests", "ReturnRequestId") { }
    }
}

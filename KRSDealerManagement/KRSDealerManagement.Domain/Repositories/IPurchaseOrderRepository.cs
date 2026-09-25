using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Queries;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        Task<PurchaseOrderListPage> QueryPageAsync(PurchaseOrderListCriteria criteria);
    }
}

using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IPurchaseOrderItemRepository : IRepository<PurchaseOrderItem>
    {
        Task<IEnumerable<PurchaseOrderItem>> GetByOrderIdAsync(int purchaseOrderId);
        Task<IEnumerable<PurchaseOrderItem>> GetPendingByOrderIdAsync(int purchaseOrderId);
    }
}

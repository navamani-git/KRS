using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class PurchaseOrderItemRepository : Repository<PurchaseOrderItem>, IPurchaseOrderItemRepository
    {
        public PurchaseOrderItemRepository(ApplicationDbContext context)
            : base(context, "PurchaseOrderItems", "OrderItemId") { }

        public async Task<IEnumerable<PurchaseOrderItem>> GetByOrderIdAsync(int purchaseOrderId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<PurchaseOrderItem>(
                    "SELECT * FROM PurchaseOrderItems WHERE PurchaseOrderId = @PurchaseOrderId ORDER BY OrderItemId",
                    new { PurchaseOrderId = purchaseOrderId },
                    transaction));
        }

        public async Task<IEnumerable<PurchaseOrderItem>> GetPendingByOrderIdAsync(int purchaseOrderId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<PurchaseOrderItem>(
                    "SELECT * FROM PurchaseOrderItems WHERE PurchaseOrderId = @PurchaseOrderId AND Status = 0 ORDER BY OrderItemId",
                    new { PurchaseOrderId = purchaseOrderId },
                    transaction));
        }
    }
}

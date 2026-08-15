using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps app PurchaseOrder entity to live PurchaseOrders schema.
    /// Writable: PurchaseOrderId, OrderNumber, SubdealerId, AccountId, TotalAmount,
    ///           PurchaseOrderStatus, VehicleCount, RejectionReason, RequestedDate,
    ///           ApprovedBy, ApprovedDate, ModifiedDate, SubdealerNotes.
    /// Computed: OrderId, TotalQuantity, Status, AdminNotes, CreatedDate.
    /// </summary>
    public class PurchaseOrderRepository : Repository<PurchaseOrder>
    {
        public PurchaseOrderRepository(ApplicationDbContext context)
            : base(context, "PurchaseOrders", "PurchaseOrderId") { }

        public override async Task<int> AddAsync(PurchaseOrder entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
INSERT INTO PurchaseOrders (
    OrderNumber, SubdealerId, AccountId,
    TotalAmount, VehicleCount, PurchaseOrderStatus,
    RejectionReason, SubdealerNotes, CreatedByDealer,
    RequestedDate, ApprovedBy, ApprovedDate, ModifiedDate
)
VALUES (
    @OrderNumber, @SubdealerId, @AccountId,
    @TotalAmount, @TotalQuantity, @PurchaseOrderStatus,
    @AdminNotes, @SubdealerNotes, @CreatedByDealer,
    @CreatedDate, @ApprovedBy, @ApprovedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                entity.OrderNumber,
                entity.SubdealerId,
                entity.AccountId,
                entity.TotalAmount,
                entity.TotalQuantity,
                PurchaseOrderStatus = MapPoStatus(entity.Status),
                entity.CreatedByDealer,
                entity.AdminNotes,
                entity.SubdealerNotes,
                entity.CreatedDate,
                entity.ApprovedBy,
                entity.ApprovedDate,
                entity.ModifiedDate
            });
        }

        public override async Task<bool> UpdateAsync(PurchaseOrder entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
UPDATE PurchaseOrders
SET OrderNumber = @OrderNumber,
    SubdealerId = @SubdealerId,
    AccountId = @AccountId,
    TotalAmount = @TotalAmount,
    VehicleCount = @TotalQuantity,
    PurchaseOrderStatus = @PurchaseOrderStatus,
    ApprovedAmount = @ApprovedAmount,
    ApprovedVehicleCount = @ApprovedVehicleCount,
    RejectionReason = @AdminNotes,
    SubdealerNotes = @SubdealerNotes,
    CreatedByDealer = @CreatedByDealer,
    ApprovedBy = @ApprovedBy,
    ApprovedDate = @ApprovedDate,
    ModifiedDate = @ModifiedDate
WHERE PurchaseOrderId = @OrderId";

            var rows = await connection.ExecuteAsync(sql, new
            {
                entity.OrderId,
                entity.OrderNumber,
                entity.SubdealerId,
                entity.AccountId,
                entity.TotalAmount,
                entity.TotalQuantity,
                PurchaseOrderStatus = MapPoStatus(entity.Status),
                entity.ApprovedAmount,
                entity.ApprovedVehicleCount,
                entity.AdminNotes,
                entity.SubdealerNotes,
                entity.CreatedByDealer,
                entity.ApprovedBy,
                entity.ApprovedDate,
                entity.ModifiedDate
            });
            return rows > 0;
        }

        public override async Task<PurchaseOrder> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(SelectSql + " WHERE PurchaseOrderId = @Id", new { Id = id });
        }

        public override async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryAsync<PurchaseOrder>(SelectSql);
        }

        private const string SelectSql = @"
SELECT
    PurchaseOrderId AS OrderId,
    OrderNumber,
    SubdealerId,
    ISNULL(AccountId, 0) AS AccountId,
    TotalAmount,
    ISNULL(ApprovedAmount, 0) AS ApprovedAmount,
    ISNULL(ApprovedVehicleCount, 0) AS ApprovedVehicleCount,
    VehicleCount AS TotalQuantity,
    ISNULL(PurchaseOrderStatus, 1) - 1 AS Status,
    ISNULL(CreatedByDealer, 0) AS CreatedByDealer,
    RejectionReason AS AdminNotes,
    SubdealerNotes,
    ApprovedBy,
    ApprovedDate,
    CAST(NULL AS datetime) AS DeliveryDate,
    RequestedDate AS CreatedDate,
    ModifiedDate
FROM PurchaseOrders";

        private static int MapPoStatus(int vehicleStatus) => vehicleStatus switch
        {
            UnifiedVehicleStatus.Submitted => 1,
            UnifiedVehicleStatus.ApprovedByDealer => 2,
            UnifiedVehicleStatus.RejectedByDealer => 3,
            _ => 1
        };
    }
}

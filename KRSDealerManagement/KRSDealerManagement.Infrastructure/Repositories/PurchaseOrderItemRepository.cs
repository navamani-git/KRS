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

        public override async Task<IEnumerable<PurchaseOrderItem>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<PurchaseOrderItem>(SelectSql, transaction: transaction));
        }

        public override async Task<PurchaseOrderItem> GetByIdAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<PurchaseOrderItem>(
                    SelectSql + " WHERE OrderItemId = @Id", new { Id = id }, transaction));
        }

        public async Task<IEnumerable<PurchaseOrderItem>> GetByOrderIdAsync(int purchaseOrderId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<PurchaseOrderItem>(
                    SelectSql + " WHERE PurchaseOrderId = @PurchaseOrderId ORDER BY OrderItemId",
                    new { PurchaseOrderId = purchaseOrderId },
                    transaction));
        }

        public async Task<IEnumerable<PurchaseOrderItem>> GetPendingByOrderIdAsync(int purchaseOrderId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<PurchaseOrderItem>(
                    SelectSql + " WHERE PurchaseOrderId = @PurchaseOrderId AND Status = 0 ORDER BY OrderItemId",
                    new { PurchaseOrderId = purchaseOrderId },
                    transaction));
        }

        private const string SelectSql = @"
SELECT
    OrderItemId, PurchaseOrderId, ModelId, ColorId, UnitPrice, Status,
    MotorNo, BatteryNo, ChargerNo, ControllerNo, ConverterNo, ChassisNumber,
    SubdealerVehicleId,
    SubdealerVehicleId AS VehicleId,
    ApprovedBy, ApprovedDate, RejectedBy, RejectedDate, Remarks,
    CreatedDate, ModifiedDate
FROM PurchaseOrderItems";

        public override async Task<bool> UpdateAsync(PurchaseOrderItem entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
UPDATE PurchaseOrderItems SET
    ModelId = @ModelId,
    ColorId = @ColorId,
    UnitPrice = @UnitPrice,
    Status = @Status,
    MotorNo = @MotorNo,
    BatteryNo = @BatteryNo,
    ChargerNo = @ChargerNo,
    ControllerNo = @ControllerNo,
    ConverterNo = @ConverterNo,
    ChassisNumber = @ChassisNumber,
    SubdealerVehicleId = @SubdealerVehicleId,
    ApprovedBy = @ApprovedBy,
    ApprovedDate = @ApprovedDate,
    RejectedBy = @RejectedBy,
    RejectedDate = @RejectedDate,
    Remarks = @Remarks,
    ModifiedDate = @ModifiedDate
WHERE OrderItemId = @OrderItemId";

                var rows = await connection.ExecuteAsync(sql, new
                {
                    entity.OrderItemId,
                    entity.ModelId,
                    entity.ColorId,
                    entity.UnitPrice,
                    entity.Status,
                    entity.MotorNo,
                    entity.BatteryNo,
                    entity.ChargerNo,
                    entity.ControllerNo,
                    entity.ConverterNo,
                    entity.ChassisNumber,
                    SubdealerVehicleId = entity.SubdealerVehicleId,
                    entity.ApprovedBy,
                    entity.ApprovedDate,
                    entity.RejectedBy,
                    entity.RejectedDate,
                    entity.Remarks,
                    ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
                }, transaction);
                return rows > 0;
            });
        }
    }
}

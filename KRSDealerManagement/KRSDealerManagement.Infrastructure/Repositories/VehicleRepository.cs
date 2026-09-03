using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps Vehicle entity to SubdealerVehicles + VehicleMasters join.
    /// </summary>
    public class VehicleRepository : Repository<Vehicle>
    {
        public VehicleRepository(ApplicationDbContext context)
            : base(context, "SubdealerVehicles", "SubdealerVehicleId") { }

        public override async Task<int> AddAsync(Vehicle entity)
        {
            if (entity.VehicleMasterId <= 0)
                throw new InvalidOperationException("VehicleMasterId is required to create a subdealer vehicle.");

            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
INSERT INTO SubdealerVehicles (
    VehicleMasterId, SubdealerId, PurchaseOrderId, VehicleStatus,
    CurrentPrice, OriginalPrice, RegistrationNumber, DeliveryDate,
    AllocatedDate, AllocatedBy, Remarks, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
)
VALUES (
    @VehicleMasterId, @SubdealerId, @PurchaseOrderId, @VehicleStatus,
    @CurrentPrice, @OriginalPrice, @RegistrationNumber, @DeliveryDate,
    @AllocatedDate, @AllocatedBy, @Remarks, @CreatedBy, @CreatedDate, @ModifiedBy, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    entity.VehicleMasterId,
                    SubdealerId = entity.SubdealerId,
                    PurchaseOrderId = entity.PurchaseOrderId,
                    VehicleStatus = entity.Status,
                    entity.CurrentPrice,
                    entity.OriginalPrice,
                    RegistrationNumber = entity.RegistrationNumber ?? "",
                    entity.DeliveryDate,
                    AllocatedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate,
                    AllocatedBy = entity.CreatedBy,
                    Remarks = entity.Notes ?? "",
                    entity.CreatedBy,
                    CreatedDate = entity.CreatedDate == default ? DateTime.UtcNow : entity.CreatedDate,
                    ModifiedBy = entity.ModifiedBy,
                    ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
                }, transaction);
            });
        }

        public override async Task<IEnumerable<Vehicle>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<Vehicle>(SelectSql, transaction: transaction));
        }

        public override async Task<Vehicle> GetByIdAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<Vehicle>(
                    SelectSql + " WHERE sv.SubdealerVehicleId = @Id",
                    new { Id = id },
                    transaction));
        }

        public async Task<IEnumerable<Vehicle>> GetByPurchaseOrderIdAsync(int purchaseOrderId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<Vehicle>(
                    SelectSql + " WHERE sv.PurchaseOrderId = @PurchaseOrderId",
                    new { PurchaseOrderId = purchaseOrderId },
                    transaction));
        }

        private const string SelectSql = @"
SELECT
    sv.SubdealerVehicleId,
    sv.SubdealerVehicleId AS VehicleId,
    sv.VehicleMasterId,
    vm.ModelId,
    vm.ColorId,
    vm.ChassisNumber,
    sv.VehicleStatus AS Status,
    sv.PurchaseOrderId,
    sv.SubdealerId,
    sv.CurrentPrice,
    sv.OriginalPrice,
    vm.MotorNo,
    vm.BatteryNo,
    vm.ChargerNo,
    vm.ControllerNo,
    vm.ConverterNo,
    0 AS ManufacturingYear,
    ISNULL(sv.Remarks, '') AS Notes,
    sv.RegistrationNumber,
    sv.DeliveryDate,
    sv.CreatedBy,
    sv.CreatedDate,
    sv.ModifiedBy,
    sv.ModifiedDate
FROM SubdealerVehicles sv
INNER JOIN VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId";

        public override async Task<bool> UpdateAsync(Vehicle entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
UPDATE SubdealerVehicles SET
    SubdealerId = @SubdealerId,
    PurchaseOrderId = @PurchaseOrderId,
    VehicleStatus = @VehicleStatus,
    CurrentPrice = @CurrentPrice,
    OriginalPrice = @OriginalPrice,
    RegistrationNumber = @RegistrationNumber,
    DeliveryDate = @DeliveryDate,
    Remarks = @Remarks,
    ModifiedBy = @ModifiedBy,
    ModifiedDate = @ModifiedDate
WHERE SubdealerVehicleId = @SubdealerVehicleId";

                var rows = await connection.ExecuteAsync(sql, new
                {
                    entity.SubdealerVehicleId,
                    entity.SubdealerId,
                    entity.PurchaseOrderId,
                    VehicleStatus = entity.Status,
                    entity.CurrentPrice,
                    entity.OriginalPrice,
                    RegistrationNumber = entity.RegistrationNumber ?? "",
                    entity.DeliveryDate,
                    Remarks = entity.Notes ?? "",
                    entity.ModifiedBy,
                    ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
                }, transaction);
                return rows > 0;
            });
        }
    }
}

using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps Vehicle entity to live Vehicles table (+ serial number columns).
    /// </summary>
    public class VehicleRepository : Repository<Vehicle>
    {
        public VehicleRepository(ApplicationDbContext context) : base(context, "Vehicles", "VehicleId") { }

        public override async Task<int> AddAsync(Vehicle entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
INSERT INTO Vehicles (
    ChassisNumber, ModelId, ColorId, VehicleStatus,
    PurchaseOrderId, SubdealerId, CurrentPrice, OriginalPrice,
    MotorNo, BatteryNo, ChargerNo, ControllerNo, ConverterNo,
    CreatedDate, ModifiedDate
)
VALUES (
    @ChassisNumber, @ModelId, @ColorId, @VehicleStatus,
    @PurchaseOrderId, @SubdealerId, @CurrentPrice, @OriginalPrice,
    @MotorNo, @BatteryNo, @ChargerNo, @ControllerNo, @ConverterNo,
    @CreatedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    entity.ChassisNumber,
                    entity.ModelId,
                    entity.ColorId,
                    VehicleStatus = entity.Status,
                    PurchaseOrderId = entity.PurchaseOrderId ?? 0,
                    SubdealerId = entity.SubdealerId ?? 0,
                    CurrentPrice = entity.CurrentPrice,
                    OriginalPrice = entity.OriginalPrice,
                    entity.MotorNo,
                    entity.BatteryNo,
                    entity.ChargerNo,
                    entity.ControllerNo,
                    entity.ConverterNo,
                    entity.CreatedDate,
                    entity.ModifiedDate
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
                await connection.QueryFirstOrDefaultAsync<Vehicle>(SelectSql + " WHERE VehicleId = @Id", new { Id = id }, transaction));
        }

        public async Task<IEnumerable<Vehicle>> GetByPurchaseOrderIdAsync(int purchaseOrderId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<Vehicle>(
                    SelectSql + " WHERE PurchaseOrderId = @PurchaseOrderId",
                    new { PurchaseOrderId = purchaseOrderId },
                    transaction));
        }

        private const string SelectSql = @"
SELECT
    VehicleId, ChassisNumber, ModelId, ColorId,
    VehicleStatus AS Status,
    PurchaseOrderId,
    CASE WHEN SubdealerId = 0 THEN NULL ELSE SubdealerId END AS SubdealerId,
    CurrentPrice, OriginalPrice,
    MotorNo, BatteryNo, ChargerNo, ControllerNo, ConverterNo,
    ISNULL(Notes, '') AS Notes,
    CreatedDate, ModifiedDate
FROM Vehicles";

        public override async Task<bool> UpdateAsync(Vehicle entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
UPDATE Vehicles SET
    ChassisNumber = @ChassisNumber,
    ModelId = @ModelId,
    ColorId = @ColorId,
    VehicleStatus = @VehicleStatus,
    PurchaseOrderId = @PurchaseOrderId,
    SubdealerId = @SubdealerId,
    CurrentPrice = @CurrentPrice,
    OriginalPrice = @OriginalPrice,
    MotorNo = @MotorNo,
    BatteryNo = @BatteryNo,
    ChargerNo = @ChargerNo,
    ControllerNo = @ControllerNo,
    ConverterNo = @ConverterNo,
    Notes = @Notes,
    ModifiedDate = @ModifiedDate
WHERE VehicleId = @VehicleId";

                var rows = await connection.ExecuteAsync(sql, new
                {
                    entity.VehicleId,
                    entity.ChassisNumber,
                    entity.ModelId,
                    entity.ColorId,
                    VehicleStatus = entity.Status,
                    PurchaseOrderId = entity.PurchaseOrderId ?? 0,
                    SubdealerId = entity.SubdealerId ?? 0,
                    entity.CurrentPrice,
                    entity.OriginalPrice,
                    entity.MotorNo,
                    entity.BatteryNo,
                    entity.ChargerNo,
                    entity.ControllerNo,
                    entity.ConverterNo,
                    Notes = entity.Notes ?? "",
                    ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
                }, transaction);
                return rows > 0;
            });
        }
    }
}

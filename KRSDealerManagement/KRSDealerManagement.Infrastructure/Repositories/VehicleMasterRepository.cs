using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class VehicleMasterRepository : Repository<VehicleMaster>, IVehicleMasterRepository
    {
        public VehicleMasterRepository(ApplicationDbContext context)
            : base(context, "VehicleMasters", "VehicleMasterId") { }

        public async Task<IEnumerable<VehicleMaster>> GetAvailableByModelColorAsync(int dealershipId, int modelId, int colorId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<VehicleMaster>(@"
SELECT * FROM VehicleMasters
WHERE DealershipId = @DealershipId
  AND ModelId = @ModelId
  AND ColorId = @ColorId
  AND IsAllocated = 0
ORDER BY ReceivedDate, ChassisNumber",
                    new { DealershipId = dealershipId, ModelId = modelId, ColorId = colorId },
                    transaction));
        }

        public async Task<VehicleMaster?> GetByChassisAsync(string chassisNumber)
        {
            var normalized = chassisNumber.Trim().ToUpperInvariant();
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<VehicleMaster>(
                    "SELECT * FROM VehicleMasters WHERE UPPER(LTRIM(RTRIM(ChassisNumber))) = @Chassis",
                    new { Chassis = normalized },
                    transaction));
        }

        public async Task<bool> ChassisExistsAsync(string chassisNumber, int? excludeVehicleMasterId = null)
        {
            var normalized = chassisNumber.Trim().ToUpperInvariant();
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var count = await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM VehicleMasters
WHERE UPPER(LTRIM(RTRIM(ChassisNumber))) = @Chassis
  AND (@ExcludeId IS NULL OR VehicleMasterId <> @ExcludeId)",
                    new { Chassis = normalized, ExcludeId = excludeVehicleMasterId },
                    transaction);
                return count > 0;
            });
        }

        public async Task SetAllocatedAsync(int vehicleMasterId, bool isAllocated, int? modifiedBy)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                await connection.ExecuteAsync(@"
UPDATE VehicleMasters SET
    IsAllocated = @IsAllocated,
    ModifiedBy = @ModifiedBy,
    ModifiedDate = @ModifiedDate
WHERE VehicleMasterId = @VehicleMasterId",
                    new
                    {
                        VehicleMasterId = vehicleMasterId,
                        IsAllocated = isAllocated,
                        ModifiedBy = modifiedBy,
                        ModifiedDate = DateTime.UtcNow
                    },
                    transaction);
                return true;
            });
        }

        public async Task AddHistoryAsync(VehicleMasterHistory history)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                await connection.ExecuteAsync(@"
INSERT INTO VehicleMasterHistory (VehicleMasterId, Action, Remarks, DetailsJson, UserId, CreatedDate)
VALUES (@VehicleMasterId, @Action, @Remarks, @DetailsJson, @UserId, @CreatedDate)",
                    new
                    {
                        history.VehicleMasterId,
                        history.Action,
                        history.Remarks,
                        history.DetailsJson,
                        history.UserId,
                        CreatedDate = history.CreatedDate == default ? DateTime.UtcNow : history.CreatedDate
                    },
                    transaction);
                return true;
            });
        }

        public async Task<IEnumerable<VehicleMasterHistory>> GetHistoryAsync(int vehicleMasterId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<VehicleMasterHistory>(@"
SELECT * FROM VehicleMasterHistory
WHERE VehicleMasterId = @VehicleMasterId
ORDER BY CreatedDate",
                    new { VehicleMasterId = vehicleMasterId },
                    transaction));
        }
    }
}

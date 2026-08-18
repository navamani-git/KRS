using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class VehicleModelColorRepository : IVehicleModelColorRepository
    {
        private readonly ApplicationDbContext _context;

        public VehicleModelColorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<T> WithConnectionAsync<T>(Func<System.Data.IDbConnection, System.Data.IDbTransaction?, Task<T>> action)
        {
            var (connection, shouldDispose) = _context.LeaseConnection();
            try
            {
                return await action(connection, _context.CurrentTransaction);
            }
            finally
            {
                if (shouldDispose)
                    connection.Dispose();
            }
        }

        public async Task<IEnumerable<int>> GetColorIdsByModelIdAsync(int modelId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
                    SELECT ColorId
                    FROM VehicleModelColors
                    WHERE ModelId = @ModelId AND IsActive = 1";
                return await connection.QueryAsync<int>(sql, new { ModelId = modelId }, transaction);
            });
        }

        public async Task<bool> IsMappedAsync(int modelId, int colorId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
                    SELECT COUNT(1)
                    FROM VehicleModelColors
                    WHERE ModelId = @ModelId AND ColorId = @ColorId AND IsActive = 1";
                var count = await connection.ExecuteScalarAsync<int>(
                    sql, new { ModelId = modelId, ColorId = colorId }, transaction);
                return count > 0;
            });
        }

        public async Task SyncMappingsAsync(int modelId, IReadOnlyList<int> colorIds, int userId)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                var existing = (await connection.QueryAsync<int>(
                    "SELECT ColorId FROM VehicleModelColors WHERE ModelId = @ModelId",
                    new { ModelId = modelId }, transaction)).ToList();

                var desired = colorIds.Distinct().ToList();
                var toRemove = existing.Except(desired).ToList();
                var toAdd = desired.Except(existing).ToList();

                if (toRemove.Count > 0)
                {
                    await connection.ExecuteAsync(
                        "DELETE FROM VehicleModelColors WHERE ModelId = @ModelId AND ColorId IN @ColorIds",
                        new { ModelId = modelId, ColorIds = toRemove }, transaction);
                }

                foreach (var colorId in toAdd)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO VehicleModelColors (ModelId, ColorId, IsActive, CreatedBy, CreatedDate, ModifiedDate)
                        VALUES (@ModelId, @ColorId, 1, @UserId, GETUTCDATE(), GETUTCDATE())",
                        new { ModelId = modelId, ColorId = colorId, UserId = userId }, transaction);
                }

                if (desired.Count > 0)
                {
                    await connection.ExecuteAsync(@"
                        UPDATE VehicleModelColors
                        SET IsActive = 1, ModifiedBy = @UserId, ModifiedDate = GETUTCDATE()
                        WHERE ModelId = @ModelId AND ColorId IN @ColorIds",
                        new { ModelId = modelId, ColorIds = desired, UserId = userId }, transaction);
                }

                return 0;
            });
        }

        public async Task<IEnumerable<VehicleModelColor>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<VehicleModelColor>(
                    "SELECT * FROM VehicleModelColors WHERE IsActive = 1", transaction: transaction));
        }
    }
}

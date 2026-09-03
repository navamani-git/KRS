using System.Data;
using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class SubdealerVehicleHistoryRepository : ISubdealerVehicleHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public SubdealerVehicleHistoryRepository(ApplicationDbContext context) => _context = context;

        private async Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, IDbTransaction?, Task<TResult>> action)
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

        public async Task AddAsync(SubdealerVehicleHistory history)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                await connection.ExecuteAsync(@"
INSERT INTO SubdealerVehicleHistory (SubdealerVehicleId, Action, Remarks, DetailsJson, UserId, CreatedDate)
VALUES (@SubdealerVehicleId, @Action, @Remarks, @DetailsJson, @UserId, @CreatedDate)",
                    new
                    {
                        history.SubdealerVehicleId,
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

        public async Task DeleteBySubdealerVehicleIdAsync(int subdealerVehicleId)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                await connection.ExecuteAsync(
                    "DELETE FROM SubdealerVehicleHistory WHERE SubdealerVehicleId = @SubdealerVehicleId",
                    new { SubdealerVehicleId = subdealerVehicleId },
                    transaction);
                return true;
            });
        }

        public async Task<IEnumerable<SubdealerVehicleHistory>> GetBySubdealerVehicleIdAsync(int subdealerVehicleId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<SubdealerVehicleHistory>(@"
SELECT * FROM SubdealerVehicleHistory
WHERE SubdealerVehicleId = @SubdealerVehicleId
ORDER BY CreatedDate",
                    new { SubdealerVehicleId = subdealerVehicleId },
                    transaction));
        }
    }
}

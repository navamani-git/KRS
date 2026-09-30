using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class GstScreenDefaultRepository : Repository<GstScreenDefault>, IGstScreenDefaultRepository
    {
        public GstScreenDefaultRepository(ApplicationDbContext context)
            : base(context, "GstScreenDefaults", "ScreenKey")
        {
        }

        public async Task<GstScreenDefault?> GetByScreenKeyAsync(string screenKey)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<GstScreenDefault>(
                    "SELECT * FROM GstScreenDefaults WHERE ScreenKey = @ScreenKey",
                    new { ScreenKey = screenKey },
                    transaction));
        }

        public async Task<IDictionary<string, int>> GetRateIdByScreenKeyAsync()
        {
            var rows = await WithConnectionAsync(async (connection, transaction) =>
                (await connection.QueryAsync<GstScreenDefault>(
                    "SELECT ScreenKey, GstRateId, ModifiedDate FROM GstScreenDefaults",
                    transaction: transaction)).ToList());

            return rows.ToDictionary(r => r.ScreenKey, r => r.GstRateId, StringComparer.OrdinalIgnoreCase);
        }

        public async Task UpsertAsync(string screenKey, int gstRateId)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
IF EXISTS (SELECT 1 FROM GstScreenDefaults WHERE ScreenKey = @ScreenKey)
    UPDATE GstScreenDefaults SET GstRateId = @GstRateId, ModifiedDate = SYSUTCDATETIME() WHERE ScreenKey = @ScreenKey;
ELSE
    INSERT INTO GstScreenDefaults (ScreenKey, GstRateId) VALUES (@ScreenKey, @GstRateId);";
                await connection.ExecuteAsync(sql, new { ScreenKey = screenKey, GstRateId = gstRateId }, transaction);
                return 0;
            });
        }
    }
}

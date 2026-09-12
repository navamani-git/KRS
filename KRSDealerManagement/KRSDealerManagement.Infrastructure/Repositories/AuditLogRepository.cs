using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(ApplicationDbContext context) : base(context, "AuditLog", "AuditLogId") { }

        public async Task<IEnumerable<AuditLog>> GetRecentAsync(DateTime sinceUtc, int take, IReadOnlyCollection<int>? userIds = null)
        {
            if (take <= 0)
                take = 50;

            return await WithConnectionAsync(async (connection, transaction) =>
            {
                var sql = @"
SELECT TOP (@Take) *
FROM AuditLog
WHERE CreatedDate >= @SinceUtc";

                if (userIds is { Count: > 0 })
                    sql += " AND UserId IN @UserIds";

                sql += " ORDER BY CreatedDate DESC";

                return await connection.QueryAsync<AuditLog>(
                    sql,
                    new { Take = take, SinceUtc = sinceUtc, UserIds = userIds },
                    transaction);
            });
        }

        /// <summary>
        /// Inserts using app columns and maps legacy NOT NULL columns (ChangedBy/ChangedDate/etc).
        /// </summary>
        public override async Task<int> AddAsync(AuditLog entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
INSERT INTO AuditLog (
    EntityType, EntityId, Action,
    ChangedBy, ChangedDate,
    UserId, UserRole,
    OldValue, NewValue, Remarks,
    OldValues, NewValues, ChangeReason,
    IpAddress, UserAgent, CreatedDate
)
VALUES (
    @EntityType, @EntityId, @Action,
    @UserId, @CreatedDate,
    @UserId, @UserRole,
    @OldValue, @NewValue, @Remarks,
    @OldValue, @NewValue, @Remarks,
    @IpAddress, @UserAgent, @CreatedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }
    }
}

using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class AuditLogRepository : Repository<AuditLog>
    {
        public AuditLogRepository(ApplicationDbContext context) : base(context, "AuditLog", "AuditLogId") { }

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

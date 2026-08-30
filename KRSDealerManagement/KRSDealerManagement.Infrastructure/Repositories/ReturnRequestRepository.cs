using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class ReturnRequestRepository : Repository<ReturnRequest>
    {
        public ReturnRequestRepository(ApplicationDbContext context)
            : base(context, "ReturnRequests", "ReturnRequestId") { }

        public override async Task<IEnumerable<ReturnRequest>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<ReturnRequest>(SelectSql, transaction: transaction));
        }

        public override async Task<ReturnRequest> GetByIdAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<ReturnRequest>(
                    SelectSql + " WHERE ReturnRequestId = @Id", new { Id = id }, transaction));
        }

        public override async Task<int> AddAsync(ReturnRequest entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
INSERT INTO ReturnRequests (
    AccountId, OrderId, SubdealerVehicleId, RefundAmount, Status,
    ReturnReason, AdminRemarks, ProcessedBy, ProcessedDate, CreatedDate, ModifiedDate
)
VALUES (
    @AccountId, @OrderId, @SubdealerVehicleId, @RefundAmount, @Status,
    @ReturnReason, @AdminRemarks, @ProcessedBy, @ProcessedDate, @CreatedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    entity.AccountId,
                    entity.OrderId,
                    SubdealerVehicleId = entity.VehicleId,
                    entity.RefundAmount,
                    entity.Status,
                    ReturnReason = entity.ReturnReason ?? "",
                    AdminRemarks = entity.AdminRemarks ?? "",
                    entity.ProcessedBy,
                    entity.ProcessedDate,
                    CreatedDate = entity.CreatedDate == default ? DateTime.UtcNow : entity.CreatedDate,
                    ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
                }, transaction);
            });
        }

        private const string SelectSql = @"
SELECT
    ReturnRequestId, AccountId, OrderId,
    SubdealerVehicleId,
    SubdealerVehicleId AS VehicleId,
    RefundAmount, Status, ReturnReason, AdminRemarks,
    ProcessedBy, ProcessedDate, CreatedDate, ModifiedDate
FROM ReturnRequests";
    }
}

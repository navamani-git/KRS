using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class CommissionRepository : Repository<Commission>
    {
        public CommissionRepository(ApplicationDbContext context) : base(context, "CommissionHistory", "CommissionId") { }

        public override async Task<int> AddAsync(Commission entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
INSERT INTO CommissionHistory (
    SubdealerVehicleId, SubdealerId, CommissionMonth, CommissionYear,
    SubmittedAmount, CommissionStatus, ApprovalReason,
    SubmittedBy, SubmittedDate, ModifiedDate
)
VALUES (
    @VehicleId, @SubdealerId, @Month, @Year,
    @CommissionAmount, @Status, @Notes,
    @SubmittedBy, @CreatedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                entity.VehicleId,
                entity.SubdealerId,
                entity.Month,
                entity.Year,
                entity.CommissionAmount,
                entity.Status,
                Notes = entity.Notes ?? "",
                entity.SubmittedBy,
                CreatedDate = entity.CreatedDate == default ? DateTime.UtcNow : entity.CreatedDate,
                ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
            });
        }

        public override async Task<bool> UpdateAsync(Commission entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
UPDATE CommissionHistory SET
    CommissionStatus = @Status,
    ApprovedAmount = @ApprovedAmount,
    ApprovalReason = @Notes,
    ApprovedBy = @ApprovedBy,
    ApprovedDate = @ApprovedDate,
    PaidDate = @PaidDate,
    RejectedBy = @RejectedBy,
    RejectedDate = @RejectedDate,
    ModifiedDate = @ModifiedDate
WHERE CommissionId = @CommissionId";

            var rows = await connection.ExecuteAsync(sql, new
            {
                entity.CommissionId,
                entity.Status,
                ApprovedAmount = entity.ApprovedAmount ?? entity.CommissionAmount,
                Notes = entity.Notes ?? "",
                entity.ApprovedBy,
                entity.ApprovedDate,
                entity.PaidDate,
                entity.RejectedBy,
                entity.RejectedDate,
                ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
            });
            return rows > 0;
        }

        public override async Task<IEnumerable<Commission>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();
            var rows = await connection.QueryAsync<Commission>(SelectSql);
            return rows.Select(Normalize);
        }

        public override async Task<Commission> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            var row = await connection.QueryFirstOrDefaultAsync<Commission>(SelectSql + " WHERE CommissionId = @Id", new { Id = id });
            return row == null ? row! : Normalize(row);
        }

        private static Commission Normalize(Commission commission)
        {
            commission.Status = CommissionStatusHelper.Normalize(
                commission.Status, commission.ApprovedDate, commission.ApprovedAmount);
            return commission;
        }

        private const string SelectSql = @"
SELECT
    CommissionId,
    SubdealerVehicleId,
    SubdealerVehicleId AS VehicleId,
    SubdealerId,
    CommissionMonth AS Month,
    CommissionYear AS Year,
    SubmittedAmount AS CommissionAmount,
    ApprovedAmount,
    CommissionStatus AS Status,
    ApprovalReason AS Notes,
    SubmittedBy,
    SubmittedDate AS CreatedDate,
    ApprovedBy,
    ApprovedDate,
    PaidDate,
    RejectedBy,
    RejectedDate,
    ModifiedDate,
    0 AS AccountId
FROM CommissionHistory";
    }
}

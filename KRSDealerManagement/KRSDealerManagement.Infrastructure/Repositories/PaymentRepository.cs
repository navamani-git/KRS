using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class PaymentRepository : Repository<Payment>
    {
        public PaymentRepository(ApplicationDbContext context) : base(context, "Payments", "PaymentId") { }

        public override async Task<Payment> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryFirstOrDefaultAsync<Payment>(SelectSql + " WHERE PaymentId = @Id", new { Id = id });
        }

        public override async Task<IEnumerable<Payment>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryAsync<Payment>(SelectSql);
        }

        public override async Task<bool> UpdateAsync(Payment entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
UPDATE Payments SET
    Amount = @Amount,
    PaymentType = @PaymentType,
    PaymentTypeId = @PaymentTypeId,
    PaymentDate = @PaymentDate,
    Status = @Status,
    SubdealerRemarks = @SubdealerRemarks,
    DealerRemarks = @DealerRemarks,
    CustomerName = @CustomerName,
    FinanceNameId = @FinanceNameId,
    VinNumber = @VinNumber,
    ProcessedBy = @ProcessedBy,
    ProcessedDate = @ProcessedDate,
    IsApplied = @IsApplied,
    TransactionId = @TransactionId,
    ModifiedDate = @ModifiedDate
WHERE PaymentId = @PaymentId";

            var rows = await connection.ExecuteAsync(sql, new
            {
                entity.PaymentId,
                entity.Amount,
                PaymentType = entity.PaymentType ?? "",
                entity.PaymentTypeId,
                entity.PaymentDate,
                entity.Status,
                SubdealerRemarks = entity.SubdealerRemarks ?? "",
                DealerRemarks = entity.DealerRemarks ?? "",
                entity.CustomerName,
                entity.FinanceNameId,
                entity.VinNumber,
                entity.ProcessedBy,
                entity.ProcessedDate,
                entity.IsApplied,
                entity.TransactionId,
                ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
            });
            return rows > 0;
        }

        private const string SelectSql = @"
SELECT
    PaymentId, AccountId, SubdealerId, Amount, PaymentType, PaymentTypeId,
    PaymentDate, Status,
    SubdealerRemarks, DealerRemarks,
    ProcessedBy, ProcessedDate,
    IsApplied, TransactionId,
    CustomerName, FinanceNameId, VinNumber,
    PaymentProofPath, PaymentProof2Path,
    CreatedDate, ModifiedDate
FROM Payments";
    }
}

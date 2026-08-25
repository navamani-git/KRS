using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class AccountTransactionCorrectionRepository : Repository<AccountTransactionCorrection>
    {
        public AccountTransactionCorrectionRepository(ApplicationDbContext context)
            : base(context, "AccountTransactionCorrections", "CorrectionId") { }

        public override async Task<int> AddAsync(AccountTransactionCorrection entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
INSERT INTO AccountTransactionCorrections (
    TransactionId, AccountId, Action,
    OldSnapshot, NewSnapshot,
    CorrectionReason, CorrectedBy, CorrectedByName, CreatedDate
)
VALUES (
    @TransactionId, @AccountId, @Action,
    @OldSnapshot, @NewSnapshot,
    @CorrectionReason, @CorrectedBy, @CorrectedByName, @CreatedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        public override async Task<IEnumerable<AccountTransactionCorrection>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryAsync<AccountTransactionCorrection>(SelectSql);
        }

        public override async Task<AccountTransactionCorrection> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryFirstOrDefaultAsync<AccountTransactionCorrection>(
                SelectSql + " WHERE CorrectionId = @Id", new { Id = id });
        }

        private const string SelectSql = @"
SELECT
    CorrectionId,
    TransactionId,
    AccountId,
    Action,
    OldSnapshot,
    NewSnapshot,
    CorrectionReason,
    CorrectedBy,
    CorrectedByName,
    CreatedDate
FROM AccountTransactionCorrections";
    }
}

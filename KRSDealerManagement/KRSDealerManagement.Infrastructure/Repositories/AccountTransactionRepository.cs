using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps AccountTransaction entity to live AccountTransactions schema.
    /// Live DB requires SubdealerId, BalanceBeforeTransaction, CreatedBy, Description;
    /// app uses AccountId, Reason, InitiatedBy, Remarks, ReferenceId/Type.
    /// </summary>
    public class AccountTransactionRepository : Repository<AccountTransaction>
    {
        public AccountTransactionRepository(ApplicationDbContext context)
            : base(context, "AccountTransactions", "TransactionId") { }

        public override async Task<int> AddAsync(AccountTransaction entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            // Resolve SubdealerId from account; compute BalanceBefore from after +/- amount
            var balanceBefore = AccountTransactionTypeHelper.EstimateBalanceBefore(
                entity.TransactionType, entity.Amount, entity.BalanceAfterTransaction);
            var description = string.IsNullOrWhiteSpace(entity.Remarks)
                ? entity.Reason
                : $"{entity.Reason} | {entity.Remarks}";

            const string sql = @"
DECLARE @subdealerId INT =
    COALESCE(
        (SELECT TOP 1 SubdealerId FROM SubdealerAccounts WHERE AccountId = @AccountId),
        (SELECT TOP 1 SubdealerId FROM AccountBalance WHERE SubdealerAccountId = @AccountId OR AccountId = @AccountId),
        0
    );

IF @subdealerId = 0
    THROW 50001, 'Cannot log transaction: SubdealerId not found for AccountId.', 1;

INSERT INTO AccountTransactions (
    SubdealerId, AccountId,
    TransactionType, Amount,
    BalanceBeforeTransaction, BalanceAfterTransaction,
    Description, Reason,
    ReferenceId, ReferenceType,
    ReferencePurchaseOrderId, ReferenceCommissionId,
    CreatedBy, InitiatedBy, CreatedDate
)
VALUES (
    @subdealerId, @AccountId,
    @TransactionType, @Amount,
    @BalanceBefore, @BalanceAfterTransaction,
    @Description, @Reason,
    @ReferenceId, @ReferenceType,
    CASE WHEN @ReferenceType IN ('PurchaseOrder', 'Order') THEN @ReferenceId END,
    CASE WHEN @ReferenceType = 'Commission' THEN @ReferenceId END,
    ISNULL(NULLIF(@InitiatedBy, 0), 1),
    ISNULL(NULLIF(@InitiatedBy, 0), 1),
    @CreatedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                entity.AccountId,
                entity.TransactionType,
                entity.Amount,
                BalanceBefore = balanceBefore,
                entity.BalanceAfterTransaction,
                Description = description,
                entity.Reason,
                entity.ReferenceId,
                entity.ReferenceType,
                entity.InitiatedBy,
                entity.CreatedDate
            });
        }

        public override async Task<AccountTransaction> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryFirstOrDefaultAsync<AccountTransaction>(
                SelectSql + " WHERE TransactionId = @Id", new { Id = id });
        }

        public override async Task<IEnumerable<AccountTransaction>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryAsync<AccountTransaction>(SelectSql);
        }

        public override async Task<bool> UpdateAsync(AccountTransaction entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
UPDATE AccountTransactions
SET AccountId = @AccountId,
    TransactionType = @TransactionType,
    Amount = @Amount,
    BalanceAfterTransaction = @BalanceAfterTransaction,
    Description = ISNULL(@Remarks, @Reason),
    Reason = @Reason,
    ReferenceId = @ReferenceId,
    ReferenceType = @ReferenceType,
    InitiatedBy = @InitiatedBy,
    CreatedBy = ISNULL(NULLIF(@InitiatedBy, 0), CreatedBy)
WHERE TransactionId = @TransactionId";

            var rows = await connection.ExecuteAsync(sql, entity);
            return rows > 0;
        }

        private const string SelectSql = @"
SELECT
    TransactionId,
    ISNULL(AccountId, 0) AS AccountId,
    TransactionType,
    Amount,
    BalanceAfterTransaction,
    ISNULL(Reason, Description) AS Reason,
    ReferenceId,
    ReferenceType,
    Remarks,
    ISNULL(InitiatedBy, CreatedBy) AS InitiatedBy,
    CreatedDate
FROM AccountTransactions";
    }
}

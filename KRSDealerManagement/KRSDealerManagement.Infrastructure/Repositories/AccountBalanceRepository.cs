using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Live DB uses AccountId (identity) as PK; entity uses BalanceId.
    /// BalanceId column is NOT NULL/non-identity — keep it synced to AccountId.
    /// </summary>
    public class AccountBalanceRepository : Repository<AccountBalance>
    {
        public AccountBalanceRepository(ApplicationDbContext context)
            : base(context, "AccountBalance", "BalanceId") { }

        public override async Task<int> AddAsync(AccountBalance entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
INSERT INTO AccountBalance (
    SubdealerId, SubdealerAccountId,
    CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance,
    LastTransactionDate, CreatedDate, ModifiedDate
)
VALUES (
    @SubdealerId, @SubdealerAccountId,
    @CurrentBalance, @ReservedAmount, @AvailableBalance, @InitialBalance,
    @LastTransactionDate, @CreatedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await connection.ExecuteScalarAsync<int>(sql, entity, transaction);
            });
        }

        public override async Task<AccountBalance> GetByIdAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
SELECT
    AccountId AS BalanceId,
    ISNULL(SubdealerAccountId, AccountId) AS SubdealerAccountId,
    SubdealerId,
    CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance,
    LastTransactionDate, CreatedDate, ModifiedDate
FROM AccountBalance
WHERE AccountId = @Id OR BalanceId = @Id";

                return await connection.QueryFirstOrDefaultAsync<AccountBalance>(sql, new { Id = id }, transaction);
            });
        }

        public async Task<AccountBalance?> GetBySubdealerAccountIdAsync(int subdealerAccountId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
SELECT
    AccountId AS BalanceId,
    ISNULL(SubdealerAccountId, AccountId) AS SubdealerAccountId,
    SubdealerId,
    CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance,
    LastTransactionDate, CreatedDate, ModifiedDate
FROM AccountBalance
WHERE SubdealerAccountId = @AccountId OR AccountId = @AccountId";

                return await connection.QueryFirstOrDefaultAsync<AccountBalance>(
                    sql, new { AccountId = subdealerAccountId }, transaction);
            });
        }

        public override async Task<IEnumerable<AccountBalance>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
SELECT
    AccountId AS BalanceId,
    ISNULL(SubdealerAccountId, AccountId) AS SubdealerAccountId,
    SubdealerId,
    CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance,
    LastTransactionDate, CreatedDate, ModifiedDate
FROM AccountBalance";

                return await connection.QueryAsync<AccountBalance>(sql, transaction: transaction);
            });
        }

        public override async Task<bool> UpdateAsync(AccountBalance entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
UPDATE AccountBalance
SET SubdealerId = @SubdealerId,
    SubdealerAccountId = @SubdealerAccountId,
    CurrentBalance = @CurrentBalance,
    ReservedAmount = @ReservedAmount,
    AvailableBalance = @AvailableBalance,
    InitialBalance = @InitialBalance,
    LastTransactionDate = @LastTransactionDate,
    ModifiedDate = @ModifiedDate
WHERE AccountId = @BalanceId OR BalanceId = @BalanceId";

                var rows = await connection.ExecuteAsync(sql, entity, transaction);
                return rows > 0;
            });
        }
    }
}

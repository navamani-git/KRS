using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class WarrantyClaimRepository : Repository<WarrantyClaim>
    {
        public WarrantyClaimRepository(ApplicationDbContext context)
            : base(context, "WarrantyClaims", "WarrantyClaimId") { }

        public async Task<IEnumerable<WarrantyClaim>> GetByAccountIdAsync(int accountId)
        {
            var (conn, dispose) = _context.LeaseConnection();
            try
            {
                return await conn.QueryAsync<WarrantyClaim>(
                    "SELECT * FROM WarrantyClaims WHERE AccountId = @accountId ORDER BY CreatedDate DESC",
                    new { accountId });
            }
            finally { if (dispose) conn.Dispose(); }
        }

        public async Task<int> GetNextSequenceForTodayAsync()
        {
            var prefix = $"WC-{DateTime.UtcNow:yyyyMMdd}-";
            var (conn, dispose) = _context.LeaseConnection();
            try
            {
                var count = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM WarrantyClaims WHERE ClaimNumber LIKE @prefix + '%'",
                    new { prefix });
                return count + 1;
            }
            finally { if (dispose) conn.Dispose(); }
        }
    }
}

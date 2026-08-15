using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps to SubdealerAccounts — one primary balance wallet per subdealer.
    /// </summary>
    public class SubdealerAccountRepository : Repository<SubdealerAccount>
    {
        public SubdealerAccountRepository(ApplicationDbContext context)
            : base(context, "SubdealerAccounts", "AccountId") { }
    }
}

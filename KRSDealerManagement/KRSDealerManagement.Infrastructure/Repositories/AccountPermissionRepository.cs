using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class AccountPermissionRepository : Repository<AccountPermission>
    {
        public AccountPermissionRepository(ApplicationDbContext context)
            : base(context, "AccountPermissions", "PermissionId") { }
    }
}

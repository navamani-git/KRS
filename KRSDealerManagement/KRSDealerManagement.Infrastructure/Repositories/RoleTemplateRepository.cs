using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class RoleTemplateRepository : Repository<RoleTemplate>, IRoleTemplateRepository
    {
        public RoleTemplateRepository(ApplicationDbContext context)
            : base(context, "RoleTemplates", "RoleTemplateId") { }

        public async Task<RoleTemplate?> GetByCodeAsync(string templateCode)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<RoleTemplate>(
                    "SELECT * FROM RoleTemplates WHERE TemplateCode = @Code",
                    new { Code = templateCode.Trim().ToUpperInvariant() },
                    transaction));
        }

        public async Task<IEnumerable<RoleTemplateMenu>> GetMenusAsync(int roleTemplateId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<RoleTemplateMenu>(@"
SELECT * FROM RoleTemplateMenus
WHERE RoleTemplateId = @RoleTemplateId
ORDER BY SortOrder, MenuKey",
                    new { RoleTemplateId = roleTemplateId },
                    transaction));
        }

        public async Task SaveMenusAsync(int roleTemplateId, IEnumerable<RoleTemplateMenu> menus)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                await connection.ExecuteAsync(
                    "DELETE FROM RoleTemplateMenus WHERE RoleTemplateId = @RoleTemplateId",
                    new { RoleTemplateId = roleTemplateId },
                    transaction);

                foreach (var menu in menus)
                {
                    await connection.ExecuteAsync(@"
INSERT INTO RoleTemplateMenus (RoleTemplateId, MenuKey, IsReadOnly, SortOrder)
VALUES (@RoleTemplateId, @MenuKey, @IsReadOnly, @SortOrder)",
                        new
                        {
                            RoleTemplateId = roleTemplateId,
                            menu.MenuKey,
                            menu.IsReadOnly,
                            menu.SortOrder
                        },
                        transaction);
                }

                return true;
            });
        }
    }
}

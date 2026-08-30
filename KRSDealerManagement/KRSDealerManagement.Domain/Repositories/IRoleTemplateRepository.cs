using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Domain.Repositories
{
    public interface IRoleTemplateRepository : IRepository<RoleTemplate>
    {
        Task<RoleTemplate?> GetByCodeAsync(string templateCode);
        Task<IEnumerable<RoleTemplateMenu>> GetMenusAsync(int roleTemplateId);
        Task SaveMenusAsync(int roleTemplateId, IEnumerable<RoleTemplateMenu> menus);
    }
}

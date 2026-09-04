using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Services
{
    public interface IRoleTemplateService
    {
        Task<IReadOnlyList<RoleTemplateCatalogItem>> GetCatalogAsync(bool includeInactive = false);
        Task<IReadOnlyDictionary<string, MenuAccessLevel>> GetDefaultMenusAsync(string? templateCode);
        string? ResolveTemplateName(string? templateCode, IReadOnlyList<RoleTemplateCatalogItem>? catalog = null);
        int MapTemplateToLegacyUserRole(string? templateCode);
        string BuildSuggestedRoleCode(string dealershipCode, string templateCode);
        bool IsReservedTemplateCode(string templateCode);
        string NormalizeTemplateCode(string templateCode);
    }

    public sealed class RoleTemplateCatalogItem
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public bool IsBuiltIn { get; init; }
        public bool IsActive { get; init; } = true;
        public int? RoleTemplateId { get; init; }
        public int MenuCount { get; set; }
        public int RoleCount { get; set; }
    }

    public class RoleTemplateService : IRoleTemplateService
    {
        private static readonly HashSet<string> ReservedCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            RoleTemplateCodes.System,
            RoleTemplateCodes.Subdealer,
            RoleTemplateCodes.Manager,
            RoleTemplateCodes.FinanceManager,
            RoleTemplateCodes.InsuranceRtoManager,
            RoleTemplateCodes.WarrantyManager,
            RoleTemplateCodes.WarrantyStaff
        };

        private readonly IUnitOfWork _unitOfWork;

        public RoleTemplateService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IReadOnlyList<RoleTemplateCatalogItem>> GetCatalogAsync(bool includeInactive = false)
        {
            var builtIn = RoleTemplateCodes.All
                .Select(t => new RoleTemplateCatalogItem
                {
                    Code = t.Code,
                    Name = t.Name,
                    Description = t.Description,
                    IsBuiltIn = true,
                    IsActive = true
                })
                .ToList();

            var custom = (await _unitOfWork.RoleTemplates.GetAllAsync())
                .Where(t => includeInactive || t.IsActive)
                .OrderBy(t => t.TemplateName)
                .Select(t => new RoleTemplateCatalogItem
                {
                    Code = t.TemplateCode,
                    Name = t.TemplateName,
                    Description = t.Description,
                    IsBuiltIn = false,
                    IsActive = t.IsActive,
                    RoleTemplateId = t.RoleTemplateId
                });

            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToList();
            var menuCounts = new Dictionary<int, int>();
            foreach (var template in await _unitOfWork.RoleTemplates.GetAllAsync())
            {
                var menus = await _unitOfWork.RoleTemplates.GetMenusAsync(template.RoleTemplateId);
                menuCounts[template.RoleTemplateId] = menus.Count();
            }

            var merged = builtIn.Concat(custom).ToList();
            foreach (var item in merged)
            {
                item.RoleCount = roles.Count(r =>
                    string.Equals(r.RoleTemplateCode, item.Code, StringComparison.OrdinalIgnoreCase));

                if (item.RoleTemplateId is int id && menuCounts.TryGetValue(id, out var count))
                    item.MenuCount = count;
                else if (item.IsBuiltIn)
                    item.MenuCount = RoleTemplateDefaults.GetDefaultMenus(item.Code).Count;
            }

            return merged;
        }

        public async Task<IReadOnlyDictionary<string, MenuAccessLevel>> GetDefaultMenusAsync(string? templateCode)
        {
            if (string.IsNullOrWhiteSpace(templateCode))
                return new Dictionary<string, MenuAccessLevel>();

            var code = templateCode.Trim().ToUpperInvariant();
            var custom = await _unitOfWork.RoleTemplates.GetByCodeAsync(code);
            if (custom != null)
            {
                var menus = await _unitOfWork.RoleTemplates.GetMenusAsync(custom.RoleTemplateId);
                return menus.ToDictionary(
                    m => m.MenuKey,
                    m => m.IsReadOnly ? MenuAccessLevel.ReadOnly : MenuAccessLevel.Full,
                    StringComparer.OrdinalIgnoreCase);
            }

            return RoleTemplateDefaults.GetDefaultMenus(code);
        }

        public string? ResolveTemplateName(string? templateCode, IReadOnlyList<RoleTemplateCatalogItem>? catalog = null)
        {
            if (string.IsNullOrWhiteSpace(templateCode))
                return null;

            catalog ??= GetCatalogAsync().GetAwaiter().GetResult();
            return catalog.FirstOrDefault(t =>
                t.Code.Equals(templateCode, StringComparison.OrdinalIgnoreCase))?.Name;
        }

        public int MapTemplateToLegacyUserRole(string? templateCode)
        {
            if (string.IsNullOrWhiteSpace(templateCode))
                return 4;

            var code = templateCode.Trim().ToUpperInvariant();
            var custom = _unitOfWork.RoleTemplates.GetByCodeAsync(code).GetAwaiter().GetResult();
            if (custom != null)
                return custom.LegacyUserRole;

            return RoleTemplateDefaults.MapTemplateToLegacyUserRole(code);
        }

        public string BuildSuggestedRoleCode(string dealershipCode, string templateCode)
        {
            var code = templateCode.Trim().ToUpperInvariant();
            if (code is RoleTemplateCodes.Manager or RoleTemplateCodes.FinanceManager
                or RoleTemplateCodes.InsuranceRtoManager or RoleTemplateCodes.WarrantyManager
                or RoleTemplateCodes.WarrantyStaff or RoleTemplateCodes.Custom)
            {
                return RoleTemplateDefaults.BuildSuggestedRoleCode(dealershipCode, code);
            }

            var dealer = Sanitize(dealershipCode);
            var suffix = Sanitize(code);
            return $"{dealer}_{suffix}";
        }

        public bool IsReservedTemplateCode(string templateCode)
            => ReservedCodes.Contains(NormalizeTemplateCode(templateCode));

        public string NormalizeTemplateCode(string templateCode)
        {
            var code = new string(templateCode.Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            while (code.Contains("__")) code = code.Replace("__", "_");
            return code.Trim('_');
        }

        private static string Sanitize(string value)
        {
            var chars = value.Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray();
            return new string(chars).Trim('_');
        }
    }
}

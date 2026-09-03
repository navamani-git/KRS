using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateStaffRoleCommandHandler : IRequestHandler<CreateStaffRoleCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IRoleTemplateService _roleTemplateService;

        public CreateStaffRoleCommandHandler(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IRoleTemplateService roleTemplateService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<int> Handle(CreateStaffRoleCommand request, CancellationToken cancellationToken)
        {
            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId)
                ?? throw new InvalidOperationException("Dealership not found.");

            var roleCode = NormalizeRoleCode(request.RoleCode);
            if (string.IsNullOrWhiteSpace(roleCode))
                throw new InvalidOperationException("Role code is required.");

            if ((await _unitOfWork.Roles.GetAllAsync()).Any(r => r.RoleCode.Equals(roleCode, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Role code '{roleCode}' already exists.");

            var menus = await ResolveMenusAsync(request.RoleTemplateCode, request.Menus);
            if (!menus.Any())
                throw new InvalidOperationException("Select at least one menu for this role.");

            var roleId = await _unitOfWork.Roles.AddAsync(new Role
            {
                RoleCode = roleCode,
                RoleName = request.RoleName.Trim(),
                Description = request.Description?.Trim(),
                RoleTemplateCode = request.RoleTemplateCode.Trim().ToUpperInvariant(),
                DealershipId = request.DealershipId,
                IsSystemRole = false,
                IsActive = true,
                SortOrder = 200,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            await CreateStaffRoleCommandHandler.SaveRoleMenusAsync(_unitOfWork, roleId, menus);

            await _auditService.LogActionAsync(
                "Role", roleId, "Create", request.CreatedBy, RoleCodes.SystemAdmin,
                JsonSerializer.Serialize(new { roleCode, request.RoleName, request.RoleTemplateCode, request.DealershipId }));

            return roleId;
        }

        internal static string NormalizeRoleCode(string value)
        {
            var code = new string(value.Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            while (code.Contains("__")) code = code.Replace("__", "_");
            return code.Trim('_');
        }

        internal async Task<List<RoleMenuPermissionInput>> ResolveMenusAsync(string templateCode, List<RoleMenuPermissionInput> submitted)
        {
            if (submitted.Count > 0)
            {
                return submitted
                    .Where(m => m.AccessLevel != MenuAccessLevel.None)
                    .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }

            var defaults = await _roleTemplateService.GetDefaultMenusAsync(templateCode);
            return defaults
                .Select(kv => new RoleMenuPermissionInput { MenuKey = kv.Key, AccessLevel = kv.Value })
                .ToList();
        }

        internal static List<RoleMenuPermissionInput> ResolveMenus(string templateCode, List<RoleMenuPermissionInput> submitted)
        {
            if (submitted.Count > 0)
            {
                return submitted
                    .Where(m => m.AccessLevel != MenuAccessLevel.None)
                    .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }

            var defaults = RoleTemplateDefaults.GetDefaultMenus(templateCode);
            return defaults
                .Select(kv => new RoleMenuPermissionInput { MenuKey = kv.Key, AccessLevel = kv.Value })
                .ToList();
        }

        internal static async Task SaveRoleMenusAsync(IUnitOfWork unitOfWork, int roleId, List<RoleMenuPermissionInput> menus)
        {
            var catalog = StaffMenuAccess.AllAdminMenus().ToDictionary(m => m.Key, m => m.Name, StringComparer.OrdinalIgnoreCase);
            var existing = (await unitOfWork.RoleMenus.GetAllAsync()).Where(m => m.RoleId == roleId).ToList();
            foreach (var row in existing)
                await unitOfWork.RoleMenus.DeleteAsync(row.RoleMenuId);

            var sort = 10;
            foreach (var menu in menus.Where(m => m.AccessLevel != MenuAccessLevel.None))
            {
                if (!catalog.TryGetValue(menu.MenuKey, out var menuName))
                    continue;

                await unitOfWork.RoleMenus.AddAsync(new RoleMenu
                {
                    RoleId = roleId,
                    MenuKey = menu.MenuKey,
                    MenuName = menuName,
                    IsAccessible = true,
                    IsReadOnly = menu.AccessLevel == MenuAccessLevel.ReadOnly,
                    SortOrder = sort,
                    CreatedDate = DateTime.UtcNow
                });
                sort += 10;
            }

            await unitOfWork.SaveChangesAsync();
        }
    }

    public class UpdateStaffRoleCommandHandler : IRequestHandler<UpdateStaffRoleCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateStaffRoleCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task Handle(UpdateStaffRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId)
                ?? throw new InvalidOperationException("Role not found.");

            if (role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("System roles cannot be edited here.");

            var menus = request.Menus
                .Where(m => m.AccessLevel != MenuAccessLevel.None)
                .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (!menus.Any())
                throw new InvalidOperationException("Select at least one menu for this role.");

            role.RoleName = request.RoleName.Trim();
            role.Description = request.Description?.Trim();
            role.IsActive = request.IsActive;
            role.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Roles.UpdateAsync(role);

            await CreateStaffRoleCommandHandler.SaveRoleMenusAsync(_unitOfWork, role.RoleId, menus);

            await _auditService.LogActionAsync(
                "Role", role.RoleId, "Update", request.ModifiedBy, RoleCodes.SystemAdmin,
                JsonSerializer.Serialize(new { role.RoleCode, role.RoleName, MenuCount = menus.Count }));
        }
    }
}

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
    public class CreateRoleTemplateCommandHandler : IRequestHandler<CreateRoleTemplateCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleTemplateService _roleTemplateService;
        private readonly IAuditService _auditService;

        public CreateRoleTemplateCommandHandler(
            IUnitOfWork unitOfWork,
            IRoleTemplateService roleTemplateService,
            IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _roleTemplateService = roleTemplateService;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateRoleTemplateCommand request, CancellationToken cancellationToken)
        {
            var code = _roleTemplateService.NormalizeTemplateCode(request.TemplateCode);
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Template code is required.");

            if (_roleTemplateService.IsReservedTemplateCode(code))
                throw new InvalidOperationException($"Template code '{code}' is reserved for a built-in template.");

            if (RoleTemplateCodes.All.Any(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Template code '{code}' conflicts with a built-in template.");

            if (await _unitOfWork.RoleTemplates.GetByCodeAsync(code) != null)
                throw new InvalidOperationException($"Template code '{code}' already exists.");

            var menus = NormalizeMenus(request.Menus);
            if (!menus.Any())
                throw new InvalidOperationException("Select at least one menu for this template.");

            var id = await _unitOfWork.RoleTemplates.AddAsync(new RoleTemplate
            {
                TemplateCode = code,
                TemplateName = request.TemplateName.Trim(),
                Description = request.Description?.Trim(),
                LegacyUserRole = request.LegacyUserRole,
                IsActive = true,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            await SaveTemplateMenusAsync(id, menus);

            await _auditService.LogActionAsync(
                "RoleTemplate", id, "Create", request.CreatedBy, RoleCodes.SystemAdmin,
                JsonSerializer.Serialize(new { code, request.TemplateName, MenuCount = menus.Count }));

            return id;
        }

        internal static List<RoleMenuPermissionInput> NormalizeMenus(List<RoleMenuPermissionInput> submitted)
            => submitted
                .Where(m => m.AccessLevel != MenuAccessLevel.None)
                .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

        internal static async Task SaveTemplateMenusAsync(
            IUnitOfWork unitOfWork,
            int roleTemplateId,
            List<RoleMenuPermissionInput> menus)
        {
            var catalog = StaffMenuAccess.AllAdminMenus().ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);
            var sort = 10;
            var rows = new List<RoleTemplateMenu>();
            foreach (var menu in menus)
            {
                if (!catalog.ContainsKey(menu.MenuKey))
                    continue;

                rows.Add(new RoleTemplateMenu
                {
                    RoleTemplateId = roleTemplateId,
                    MenuKey = menu.MenuKey,
                    IsReadOnly = menu.AccessLevel == MenuAccessLevel.ReadOnly,
                    SortOrder = sort
                });
                sort += 10;
            }

            await unitOfWork.RoleTemplates.SaveMenusAsync(roleTemplateId, rows);
            await unitOfWork.SaveChangesAsync();
        }

        private Task SaveTemplateMenusAsync(int roleTemplateId, List<RoleMenuPermissionInput> menus)
            => SaveTemplateMenusAsync(_unitOfWork, roleTemplateId, menus);
    }

    public class UpdateRoleTemplateCommandHandler : IRequestHandler<UpdateRoleTemplateCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateRoleTemplateCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task Handle(UpdateRoleTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _unitOfWork.RoleTemplates.GetByIdAsync(request.RoleTemplateId)
                ?? throw new InvalidOperationException("Role template not found.");

            var menus = CreateRoleTemplateCommandHandler.NormalizeMenus(request.Menus);
            if (!menus.Any())
                throw new InvalidOperationException("Select at least one menu for this template.");

            template.TemplateName = request.TemplateName.Trim();
            template.Description = request.Description?.Trim();
            template.LegacyUserRole = request.LegacyUserRole;
            template.IsActive = request.IsActive;
            template.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RoleTemplates.UpdateAsync(template);

            await CreateRoleTemplateCommandHandler.SaveTemplateMenusAsync(_unitOfWork, template.RoleTemplateId, menus);

            await _auditService.LogActionAsync(
                "RoleTemplate", template.RoleTemplateId, "Update", request.ModifiedBy, RoleCodes.SystemAdmin,
                JsonSerializer.Serialize(new { template.TemplateCode, template.TemplateName, MenuCount = menus.Count }));
        }
    }

    public class UpsertBuiltInRoleTemplateOverrideCommandHandler : IRequestHandler<UpsertBuiltInRoleTemplateOverrideCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpsertBuiltInRoleTemplateOverrideCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(UpsertBuiltInRoleTemplateOverrideCommand request, CancellationToken cancellationToken)
        {
            var code = request.TemplateCode.Trim().ToUpperInvariant();
            if (!RoleTemplateCodes.All.Any(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Only built-in template codes can be configured here.");

            var menus = CreateRoleTemplateCommandHandler.NormalizeMenus(request.Menus);
            if (!menus.Any())
                throw new InvalidOperationException("Select at least one menu for this template.");

            var legacyRole = RoleTemplateDefaults.MapTemplateToLegacyUserRole(code);
            var existing = await _unitOfWork.RoleTemplates.GetByCodeAsync(code);
            if (existing == null)
            {
                var id = await _unitOfWork.RoleTemplates.AddAsync(new RoleTemplate
                {
                    TemplateCode = code,
                    TemplateName = request.TemplateName.Trim(),
                    Description = request.Description?.Trim(),
                    LegacyUserRole = legacyRole,
                    IsActive = request.IsActive,
                    CreatedBy = request.ModifiedBy,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
                await CreateRoleTemplateCommandHandler.SaveTemplateMenusAsync(_unitOfWork, id, menus);
                await _auditService.LogActionAsync(
                    "RoleTemplate", id, "ConfigureBuiltIn", request.ModifiedBy, RoleCodes.SystemAdmin,
                    JsonSerializer.Serialize(new { code, request.TemplateName, MenuCount = menus.Count }));
                return id;
            }

            existing.TemplateName = request.TemplateName.Trim();
            existing.Description = request.Description?.Trim();
            existing.LegacyUserRole = legacyRole;
            existing.IsActive = request.IsActive;
            existing.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RoleTemplates.UpdateAsync(existing);
            await CreateRoleTemplateCommandHandler.SaveTemplateMenusAsync(_unitOfWork, existing.RoleTemplateId, menus);
            await _auditService.LogActionAsync(
                "RoleTemplate", existing.RoleTemplateId, "ConfigureBuiltIn", request.ModifiedBy, RoleCodes.SystemAdmin,
                JsonSerializer.Serialize(new { code, request.TemplateName, MenuCount = menus.Count }));
            return existing.RoleTemplateId;
        }
    }
}

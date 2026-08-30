using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetRoleTemplatesQueryHandler : IRequestHandler<GetRoleTemplatesQuery, IReadOnlyList<RoleTemplateCatalogItem>>
    {
        private readonly IRoleTemplateService _roleTemplateService;

        public GetRoleTemplatesQueryHandler(IRoleTemplateService roleTemplateService)
            => _roleTemplateService = roleTemplateService;

        public Task<IReadOnlyList<RoleTemplateCatalogItem>> Handle(GetRoleTemplatesQuery request, CancellationToken cancellationToken)
            => _roleTemplateService.GetCatalogAsync(request.IncludeInactive);
    }

    public class GetRoleTemplateByIdQueryHandler : IRequestHandler<GetRoleTemplateByIdQuery, RoleTemplateDetailDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRoleTemplateByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<RoleTemplateDetailDto?> Handle(GetRoleTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            var template = await _unitOfWork.RoleTemplates.GetByIdAsync(request.RoleTemplateId);
            if (template == null) return null;

            var menus = (await _unitOfWork.RoleTemplates.GetMenusAsync(template.RoleTemplateId))
                .Select(m => new RoleMenuPermissionInput
                {
                    MenuKey = m.MenuKey,
                    AccessLevel = m.IsReadOnly ? MenuAccessLevel.ReadOnly : MenuAccessLevel.Full
                })
                .ToList();

            return new RoleTemplateDetailDto
            {
                RoleTemplateId = template.RoleTemplateId,
                TemplateCode = template.TemplateCode,
                TemplateName = template.TemplateName,
                Description = template.Description,
                LegacyUserRole = template.LegacyUserRole,
                IsActive = template.IsActive,
                Menus = menus
            };
        }
    }

    public class GetRoleTemplateDefaultMenusQueryHandler : IRequestHandler<GetRoleTemplateDefaultMenusQuery, Dictionary<string, int>>
    {
        private readonly IRoleTemplateService _roleTemplateService;

        public GetRoleTemplateDefaultMenusQueryHandler(IRoleTemplateService roleTemplateService)
            => _roleTemplateService = roleTemplateService;

        public async Task<Dictionary<string, int>> Handle(GetRoleTemplateDefaultMenusQuery request, CancellationToken cancellationToken)
        {
            var defaults = await _roleTemplateService.GetDefaultMenusAsync(request.TemplateCode);
            return defaults.ToDictionary(kv => kv.Key, kv => (int)kv.Value);
        }
    }
}

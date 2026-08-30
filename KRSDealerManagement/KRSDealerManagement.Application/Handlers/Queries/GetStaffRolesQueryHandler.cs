using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetStaffRolesQueryHandler : IRequestHandler<GetStaffRolesQuery, IEnumerable<StaffRoleDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleTemplateService _roleTemplateService;

        public GetStaffRolesQueryHandler(IUnitOfWork unitOfWork, IRoleTemplateService roleTemplateService)
        {
            _unitOfWork = unitOfWork;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<IEnumerable<StaffRoleDto>> Handle(GetStaffRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = (await _unitOfWork.Roles.GetAllAsync()).AsEnumerable();
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var roleMenus = (await _unitOfWork.RoleMenus.GetAllAsync()).ToList();
            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync()).Where(a => a.IsActive).ToList();
            var catalog = await _roleTemplateService.GetCatalogAsync(includeInactive: true);

            roles = roles.Where(r =>
                !r.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                && !r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));

            if (request.AssignableOnly)
                roles = roles.Where(r => r.IsActive && !r.IsSystemRole);

            if (request.IsActive.HasValue)
                roles = roles.Where(r => r.IsActive == request.IsActive.Value);

            if (request.DealershipId.HasValue)
                roles = roles.Where(r => r.DealershipId == request.DealershipId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                roles = roles.Where(r =>
                    r.RoleName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.RoleCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (r.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return roles
                .OrderBy(r => r.DealershipId)
                .ThenBy(r => r.RoleName)
                .Select(r => MapRole(r, dealerships, roleMenus, assignments, catalog))
                .ToList();
        }

        internal static StaffRoleDto MapRole(
            Domain.Entities.Role role,
            Dictionary<int, Domain.Entities.Dealership> dealerships,
            List<Domain.Entities.RoleMenu> roleMenus,
            List<Domain.Entities.UserOrgRole> assignments,
            IReadOnlyList<RoleTemplateCatalogItem> catalog)
        {
            dealerships.TryGetValue(role.DealershipId ?? 0, out var dealership);
            var menus = roleMenus.Where(m => m.RoleId == role.RoleId && m.IsAccessible).OrderBy(m => m.SortOrder).ToList();
            var template = catalog.FirstOrDefault(t =>
                t.Code.Equals(role.RoleTemplateCode ?? "", StringComparison.OrdinalIgnoreCase));
            var templateName = template?.Name
                ?? RoleTemplateCodes.All.FirstOrDefault(t =>
                    t.Code.Equals(role.RoleTemplateCode ?? "", StringComparison.OrdinalIgnoreCase)).Name;

            return new StaffRoleDto
            {
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                RoleTemplateCode = role.RoleTemplateCode,
                RoleTemplateName = templateName,
                DealershipId = role.DealershipId,
                DealershipName = dealership?.DealershipName,
                IsSystemRole = role.IsSystemRole,
                IsActive = role.IsActive,
                UserCount = assignments.Count(a => a.RoleId == role.RoleId),
                MenuCount = menus.Count,
                Menus = menus.Select(m => new RoleMenuPermissionDto
                {
                    MenuKey = m.MenuKey,
                    MenuName = m.MenuName,
                    AccessLevel = m.IsReadOnly ? MenuAccessLevel.ReadOnly : MenuAccessLevel.Full
                }).ToList()
            };
        }
    }

    public class GetStaffRoleByIdQueryHandler : IRequestHandler<GetStaffRoleByIdQuery, StaffRoleDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleTemplateService _roleTemplateService;

        public GetStaffRoleByIdQueryHandler(IUnitOfWork unitOfWork, IRoleTemplateService roleTemplateService)
        {
            _unitOfWork = unitOfWork;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<StaffRoleDto?> Handle(GetStaffRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId);
            if (role == null) return null;

            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var roleMenus = (await _unitOfWork.RoleMenus.GetAllAsync()).ToList();
            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync()).Where(a => a.IsActive).ToList();
            var catalog = await _roleTemplateService.GetCatalogAsync(includeInactive: true);
            return GetStaffRolesQueryHandler.MapRole(role, dealerships, roleMenus, assignments, catalog);
        }
    }
}

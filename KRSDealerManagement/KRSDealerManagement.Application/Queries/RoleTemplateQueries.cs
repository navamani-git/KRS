using MediatR;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Queries
{
    public class GetRoleTemplatesQuery : IRequest<IReadOnlyList<Services.RoleTemplateCatalogItem>>
    {
        public bool IncludeInactive { get; set; }
    }

    public class GetRoleTemplateByIdQuery : IRequest<RoleTemplateDetailDto?>
    {
        public int RoleTemplateId { get; set; }
    }

    public class GetRoleTemplateDefaultMenusQuery : IRequest<Dictionary<string, int>>
    {
        public string TemplateCode { get; set; } = "";
    }

    public class RoleTemplateDetailDto
    {
        public int RoleTemplateId { get; set; }
        public string TemplateCode { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public int LegacyUserRole { get; set; } = 4;
        public bool IsActive { get; set; } = true;
        public List<RoleMenuPermissionInput> Menus { get; set; } = new();
    }
}

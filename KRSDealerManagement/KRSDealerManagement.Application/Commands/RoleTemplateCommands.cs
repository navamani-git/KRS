using MediatR;
using KRSDealerManagement.Application.Queries;

namespace KRSDealerManagement.Application.Commands
{
    public class CreateRoleTemplateCommand : IRequest<int>
    {
        public string TemplateCode { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public int LegacyUserRole { get; set; } = 4;
        public List<RoleMenuPermissionInput> Menus { get; set; } = new();
        public int CreatedBy { get; set; }
    }

    public class UpdateRoleTemplateCommand : IRequest
    {
        public int RoleTemplateId { get; set; }
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public int LegacyUserRole { get; set; } = 4;
        public bool IsActive { get; set; } = true;
        public List<RoleMenuPermissionInput> Menus { get; set; } = new();
        public int ModifiedBy { get; set; }
    }

    public class UpsertBuiltInRoleTemplateOverrideCommand : IRequest<int>
    {
        public string TemplateCode { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<RoleMenuPermissionInput> Menus { get; set; } = new();
        public int ModifiedBy { get; set; }
    }
}

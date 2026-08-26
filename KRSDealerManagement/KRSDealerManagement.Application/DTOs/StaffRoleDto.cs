using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.DTOs
{
    public class RoleMenuPermissionDto
    {
        public string MenuKey { get; set; } = "";
        public string MenuName { get; set; } = "";
        public MenuAccessLevel AccessLevel { get; set; }
    }

    public class StaffRoleDto
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public string? RoleTemplateCode { get; set; }
        public string? RoleTemplateName { get; set; }
        public int? DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int MenuCount { get; set; }
        public List<RoleMenuPermissionDto> Menus { get; set; } = new();
    }
}

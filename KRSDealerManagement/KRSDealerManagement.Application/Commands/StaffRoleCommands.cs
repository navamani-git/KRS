using MediatR;
using KRSDealerManagement.Application.Queries;

namespace KRSDealerManagement.Application.Commands
{
    public class CreateStaffRoleCommand : IRequest<int>
    {
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public string RoleTemplateCode { get; set; } = "";
        public int DealershipId { get; set; }
        public List<RoleMenuPermissionInput> Menus { get; set; } = new();
        public int CreatedBy { get; set; }
    }

    public class UpdateStaffRoleCommand : IRequest
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<RoleMenuPermissionInput> Menus { get; set; } = new();
        public int ModifiedBy { get; set; }
    }
}

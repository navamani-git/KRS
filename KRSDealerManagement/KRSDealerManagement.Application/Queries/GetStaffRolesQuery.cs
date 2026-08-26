using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Queries
{
    public class GetStaffRolesQuery : IRequest<IEnumerable<StaffRoleDto>>
    {
        public int? DealershipId { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; }
        public bool AssignableOnly { get; set; }
    }

    public class GetStaffRoleByIdQuery : IRequest<StaffRoleDto?>
    {
        public int RoleId { get; set; }
    }

    public class RoleMenuPermissionInput
    {
        public string MenuKey { get; set; } = "";
        public MenuAccessLevel AccessLevel { get; set; }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetStaffUsersQuery : IRequest<IEnumerable<StaffUserDto>>
    {
        public int? RoleId { get; set; }
        public int? DealershipId { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; }
    }
}

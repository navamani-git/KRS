using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class CreateStaffUserCommand : IRequest<int>
    {
        public required string FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public int RoleId { get; set; }
        public int DealershipId { get; set; }
        public int CreatedBy { get; set; }
    }
}

using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class UpdateMyLoginCredentialsCommand : IRequest<bool>
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}

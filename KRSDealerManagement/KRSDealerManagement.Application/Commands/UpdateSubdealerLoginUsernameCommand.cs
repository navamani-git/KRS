using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class UpdateSubdealerLoginUsernameCommand : IRequest<bool>
    {
        public int LoginUserId { get; set; }
        public int SubDealerId { get; set; }
        public required string Username { get; set; }
        public int UpdatedBy { get; set; }
    }
}

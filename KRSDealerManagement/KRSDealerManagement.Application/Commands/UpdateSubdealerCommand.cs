using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class UpdateSubdealerCommand : IRequest<bool>
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string SubdealerName { get; set; }
        public required string Location { get; set; }
        public required string Email { get; set; }
        public required string PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? SalesRepMobile { get; set; }
        public string? ServiceRepMobile { get; set; }
        public bool IsActive { get; set; }
        public int DealershipId { get; set; }
        public int UpdatedBy { get; set; }
    }
}

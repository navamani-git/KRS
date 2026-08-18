using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>Create a unique subdealer business org under a dealership (no login yet).</summary>
    public class CreateSubdealerCommand : IRequest<int>
    {
        public required string SubdealerName { get; set; }
        public required string Email { get; set; }
        public required string Location { get; set; }
        public required string PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? SalesRepMobile { get; set; }
        public string? ServiceRepMobile { get; set; }
        public int DealershipId { get; set; }
        public int CreatedBy { get; set; }
    }
}

using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class AdjustSubdealerAccountCommand : IRequest<bool>
    {
        public int SubdealerId { get; set; }
        public required string AdjustmentType { get; set; }
        public decimal Amount { get; set; }
        public required string Description { get; set; }
        public string? Remarks { get; set; }
        public int AdjustedBy { get; set; }
    }
}

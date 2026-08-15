using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class RejectCommissionCommand : IRequest<bool>
    {
        public int CommissionId { get; set; }
        public int RejectedBy { get; set; }
        public required string Remarks { get; set; }
    }
}

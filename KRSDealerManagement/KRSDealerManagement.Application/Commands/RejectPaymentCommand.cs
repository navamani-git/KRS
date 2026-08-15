using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Reject payment from subdealer
    /// Will log to AuditLog with rejection reason
    /// </summary>
    public class RejectPaymentCommand : IRequest<bool>
    {
        public int PaymentId { get; set; }
        public int RejectedBy { get; set; }
        public required string Remarks { get; set; }
    }
}

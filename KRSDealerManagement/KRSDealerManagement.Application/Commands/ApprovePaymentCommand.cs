using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Approve payment from subdealer
    /// Will log to AuditLog and optionally apply to balance
    /// </summary>
    public class ApprovePaymentCommand : IRequest<bool>
    {
        public int PaymentId { get; set; }
        public int ApprovedBy { get; set; }
        public required string Remarks { get; set; }
        public bool ApplyToBalance { get; set; } // Whether to credit account immediately
    }
}

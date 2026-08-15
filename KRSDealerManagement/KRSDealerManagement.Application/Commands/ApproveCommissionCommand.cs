using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Approve commission and credit to account
    /// Will log to AuditLog and create AccountTransaction (credit)
    /// </summary>
    public class ApproveCommissionCommand : IRequest<bool>
    {
        public int CommissionId { get; set; }
        public int ApprovedBy { get; set; }
        public string? Remarks { get; set; }
    }
}

using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Approve return request and refund amount
    /// Will log to AuditLog and create AccountTransaction (credit)
    /// </summary>
    public class ApproveReturnRequestCommand : IRequest<bool>
    {
        public int ReturnRequestId { get; set; }
        public int ApprovedBy { get; set; }
        public decimal RefundAmount { get; set; }
        public string Remarks { get; set; } = "";
        /// <summary>When set, vehicle is reassigned to this subdealer. Null = dealer showroom stock.</summary>
        public int? ReassignToSubdealerId { get; set; }
    }
}

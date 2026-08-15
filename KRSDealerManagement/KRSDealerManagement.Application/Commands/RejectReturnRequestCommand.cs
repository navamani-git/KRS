using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Reject return request
    /// Will log to AuditLog, no refund issued
    /// </summary>
    public class RejectReturnRequestCommand : IRequest<bool>
    {
        public int ReturnRequestId { get; set; }
        public int RejectedBy { get; set; }
        public required string Remarks { get; set; }
    }
}

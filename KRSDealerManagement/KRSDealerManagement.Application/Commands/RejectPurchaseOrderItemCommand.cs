using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Reject purchase order (full order). VehicleId is optional for future per-item rejection.
    /// Releases reserved amount and logs AuditLog.
    /// </summary>
    public class RejectPurchaseOrderItemCommand : IRequest<bool>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; } // 0 = entire order
        public decimal Amount { get; set; }
        public int RejectedBy { get; set; }
        public required string Remarks { get; set; }
    }
}

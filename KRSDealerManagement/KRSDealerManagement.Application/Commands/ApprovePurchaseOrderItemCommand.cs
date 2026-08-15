using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Approve purchase order (full order). VehicleId is optional for future per-item approval.
    /// Debits account balance and logs AuditLog + AccountTransaction.
    /// </summary>
    public class ApprovePurchaseOrderItemCommand : IRequest<bool>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; } // 0 = entire order
        public decimal Amount { get; set; }
        public int ApprovedBy { get; set; } // Admin/Dealer UserId
        public required string Remarks { get; set; }
    }
}

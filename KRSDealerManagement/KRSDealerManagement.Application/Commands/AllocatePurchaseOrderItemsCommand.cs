using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Dealer allocates/approves selected order line items with component serial numbers.
    /// </summary>
    public class AllocatePurchaseOrderItemsCommand : IRequest<bool>
    {
        public int OrderId { get; set; }
        public int ApprovedBy { get; set; }
        public string? Remarks { get; set; }
        public required List<AllocateOrderItemDto> Items { get; set; }
    }

    public class AllocateOrderItemDto
    {
        public int OrderItemId { get; set; }
        public bool Approve { get; set; }
        public int? VehicleMasterId { get; set; }
        public string? ChassisNumber { get; set; }
        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Reject remaining (or selected) pending line items and release reserved balance.
    /// </summary>
    public class RejectPurchaseOrderItemsCommand : IRequest<bool>
    {
        public int OrderId { get; set; }
        public int RejectedBy { get; set; }
        public required string Remarks { get; set; }
        /// <summary>Empty = reject all pending items</summary>
        public List<int>? OrderItemIds { get; set; }
    }
}

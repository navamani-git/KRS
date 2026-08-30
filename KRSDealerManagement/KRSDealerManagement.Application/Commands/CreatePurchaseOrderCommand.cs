using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create purchase order from subdealer
    /// Will log order creation and reserve amount from balance
    /// </summary>
    public class CreatePurchaseOrderCommand : IRequest<int>
    {
        public int AccountId { get; set; }
        public int SubdealerId { get; set; }
        public required List<OrderItem> Items { get; set; }
        public string? SubdealerNotes { get; set; }
        public string? AdminNotes { get; set; }
        public int CreatedBy { get; set; }
        public bool AutoApprove { get; set; }
    }

    /// <summary>
    /// Individual order item
    /// </summary>
    public class OrderItem
    {
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>Vehicle master row when staff auto-approves or allocates from dealer stock.</summary>
        public int? VehicleMasterId { get; set; }

        /// <summary>Serial numbers — populated from master on allocation (read-only display).</summary>
        public string? ChassisNumber { get; set; }
        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }
    }
}

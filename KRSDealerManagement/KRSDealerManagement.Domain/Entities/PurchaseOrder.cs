using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a purchase order from subdealer for vehicles
    /// Aggregate root for purchase order domain
    /// </summary>
    public class PurchaseOrder
    {
        /// <summary>
        /// Unique identifier (aggregate root ID)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Reference to SubdealerAccount placing order
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Subdealer User ID (denormalized for quick access)
        /// </summary>
        public int SubdealerId { get; set; }

        /// <summary>
        /// Unique order number for reference
        /// Format: ORD-2024-00001
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// Total quantity of vehicles ordered
        /// </summary>
        public int TotalQuantity { get; set; }

        /// <summary>
        /// Total order value in rupees
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Amount approved/allocated so far (live column ApprovedAmount)</summary>
        public decimal ApprovedAmount { get; set; }

        /// <summary>Vehicles approved so far (live column ApprovedVehicleCount)</summary>
        public int ApprovedVehicleCount { get; set; }

        /// <summary>True when staff/dealer created the order on behalf of subdealer (auto-approved).</summary>
        public bool CreatedByDealer { get; set; }

        /// <summary>
        /// Legacy header status — derived from linked vehicles in queries; not authoritative.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Admin notes/remarks
        /// </summary>
        public string AdminNotes { get; set; }

        /// <summary>
        /// Subdealer notes/special requests
        /// </summary>
        public string SubdealerNotes { get; set; }

        /// <summary>
        /// Admin who approved the order (null if pending/rejected)
        /// </summary>
        public int? ApprovedBy { get; set; }

        /// <summary>
        /// When order was approved (UTC, null if not approved)
        /// </summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// When order is expected to be delivered
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// Order creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if order can be approved (is pending)
        /// </summary>
        public bool CanBeApproved()
        {
            return Status == (int)PurchaseOrderStatusEnum.Pending;
        }

        /// <summary>
        /// Check if order can be rejected (is pending)
        /// </summary>
        public bool CanBeRejected()
        {
            return Status == (int)PurchaseOrderStatusEnum.Pending;
        }

        /// <summary>
        /// Approve the order
        /// </summary>
        public void Approve(int approverUserId)
        {
            if (!CanBeApproved())
                throw new InvalidOperationException($"Cannot approve order in {GetStatusDisplay()} status");

            Status = (int)PurchaseOrderStatusEnum.Approved;
            ApprovedBy = approverUserId;
            ApprovedDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Reject the order
        /// </summary>
        public void Reject()
        {
            if (!CanBeRejected())
                throw new InvalidOperationException($"Cannot reject order in {GetStatusDisplay()} status");

            Status = (int)PurchaseOrderStatusEnum.Rejected;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark order as delivered
        /// </summary>
        public void MarkAsDelivered()
        {
            if (Status != (int)PurchaseOrderStatusEnum.Approved)
                throw new InvalidOperationException("Only approved orders can be marked delivered");

            Status = (int)PurchaseOrderStatusEnum.Delivered;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Get status as display text
        /// </summary>
        public string GetStatusDisplay()
        {
            return ((PurchaseOrderStatusEnum)Status).ToString();
        }

        /// <summary>
        /// Check if order is in final status (approved or rejected)
        /// </summary>
        public bool IsFinal()
        {
            return Status != (int)PurchaseOrderStatusEnum.Pending;
        }

        /// <summary>
        /// Get order display string
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"{OrderNumber} | {TotalQuantity} vehicles | ₹{TotalAmount:N2} | {GetStatusDisplay()}";
        }
    }
}

using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Purchase Order Data Transfer Object
    /// </summary>
    public class PurchaseOrderDto
    {
        public int OrderId { get; set; }
        public int AccountId { get; set; }
        public required string AccountName { get; set; }
        public int SubdealerId { get; set; }
        public required string SubdealerName { get; set; }
        public required string OrderNumber { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public bool CreatedByDealer { get; set; }
        public int PendingItemCount { get; set; }
        public int ApprovedItemCount { get; set; }
        public string? AdminNotes { get; set; }
        public string? SubdealerNotes { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? LastAllocatedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetStatusDisplay()
        {
            if (!string.IsNullOrWhiteSpace(StatusName))
                return StatusName;
            return Status switch
            {
                UnifiedVehicleStatus.Submitted => "Submitted",
                UnifiedVehicleStatus.ApprovedByDealer => "Approved By Dealer",
                UnifiedVehicleStatus.RejectedByDealer => "Rejected By Dealer",
                _ => "Unknown"
            };
        }

        public string GetBadgeClass()
        {
            if (Status == UnifiedVehicleStatus.Submitted && ApprovedItemCount > 0 && PendingItemCount > 0)
                return "bg-info text-dark";
            if (!string.IsNullOrWhiteSpace(StatusBadgeClass))
                return StatusBadgeClass;
            return Status switch
            {
                UnifiedVehicleStatus.Submitted => "bg-warning text-dark",
                UnifiedVehicleStatus.ApprovedByDealer => "bg-success",
                UnifiedVehicleStatus.RejectedByDealer => "bg-danger",
                UnifiedVehicleStatus.ReturnRequested => "bg-warning text-dark",
                UnifiedVehicleStatus.ReturnApproved => "bg-info",
                UnifiedVehicleStatus.Delivered => "bg-success",
                _ => "bg-secondary"
            };
        }

        public bool CanBeApproved()
        {
            return Status == UnifiedVehicleStatus.Submitted || PendingItemCount > 0;
        }

        public bool CanBeRejected()
        {
            return Status == UnifiedVehicleStatus.Submitted || PendingItemCount > 0;
        }

        public string GetDisplayInfo()
        {
            return $"{OrderNumber} | {TotalQuantity} vehicles | ₹{TotalAmount:N2} | {GetStatusDisplay()}";
        }
    }
}

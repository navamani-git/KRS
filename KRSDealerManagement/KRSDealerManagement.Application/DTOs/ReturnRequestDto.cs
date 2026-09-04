using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Return Request Data Transfer Object
    /// </summary>
    public class ReturnRequestDto
    {
        public int ReturnRequestId { get; set; }
        public int AccountId { get; set; }
        public required string AccountName { get; set; }
        public int OrderId { get; set; }
        public required string OrderNumber { get; set; }
        public int VehicleId { get; set; }
        public required string VehicleChassisNumber { get; set; }
        public int? SubdealerUserId { get; set; }
        public string? SubdealerName { get; set; }
        public decimal RefundAmount { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public required string ReturnReason { get; set; }
        public string? AdminRemarks { get; set; }
        public int? ProcessedBy { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? RefundCreditedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetStatusDisplay()
            => !string.IsNullOrWhiteSpace(StatusName)
                ? StatusName
                : Status switch
                {
                    UnifiedVehicleStatus.ReturnRequested => "Return Requested",
                    UnifiedVehicleStatus.ReturnApproved => "Return Approved",
                    UnifiedVehicleStatus.ReturnCancelled => "Return Cancelled",
                    _ => "Unknown"
                };

        public string GetBadgeClass()
            => !string.IsNullOrWhiteSpace(StatusBadgeClass)
                ? StatusBadgeClass
                : Status switch
                {
                    UnifiedVehicleStatus.ReturnRequested => "bg-warning text-dark",
                    UnifiedVehicleStatus.ReturnApproved => "bg-info",
                    UnifiedVehicleStatus.ReturnCancelled => "bg-secondary",
                    _ => "bg-secondary"
                };

        public bool CanBeApproved() => Status == UnifiedVehicleStatus.ReturnRequested;

        public bool CanBeRejected() => Status == UnifiedVehicleStatus.ReturnRequested;

        /// <summary>Approved return; vehicle is in dealer showroom and can be allocated again.</summary>
        public bool CanAllocateToSubdealer { get; set; }

        public string GetDisplayInfo()
        {
            return $"Return: ₹{RefundAmount:N2} | {GetStatusDisplay()}";
        }

        public bool IsFinal()
        {
            return Status is UnifiedVehicleStatus.ReturnApproved or UnifiedVehicleStatus.ReturnCancelled;
        }
    }
}

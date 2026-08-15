using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.DTOs
{
    public class PurchaseOrderItemDto
    {
        public int OrderItemId { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ModelId { get; set; }
        public string ModelName { get; set; } = "";
        public int ColorId { get; set; }
        public string ColorName { get; set; } = "";
        public decimal UnitPrice { get; set; }

        /// <summary>Line-item allocation: 0=Pending, 1=Approved, 2=Rejected (unchanged from original flow).</summary>
        public int Status { get; set; }

        /// <summary>Unified vehicle lifecycle status (1–14) for display only.</summary>
        public int VehicleStatus { get; set; }

        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }
        public string? ChassisNumber { get; set; }
        public int? VehicleId { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }

        public DateTime? GetAllocatedOn() => ApprovedDate ?? RejectedDate;

        public string GetStatusDisplay()
        {
            if (!string.IsNullOrWhiteSpace(StatusName))
                return StatusName;

            if (VehicleStatus > 0)
            {
                return VehicleStatus switch
                {
                    UnifiedVehicleStatus.Submitted => "Submitted",
                    UnifiedVehicleStatus.ApprovedByDealer => "Approved By Dealer",
                    UnifiedVehicleStatus.RejectedByDealer => "Rejected By Dealer",
                    _ => VehicleStatus.ToString()
                };
            }

            return Status switch
            {
                0 => "Pending",
                1 => "Approved",
                2 => "Rejected",
                _ => "Unknown"
            };
        }

        public string GetBadgeClass()
        {
            if (!string.IsNullOrWhiteSpace(StatusBadgeClass))
                return StatusBadgeClass;

            if (VehicleStatus > 0 && StatusName == null)
            {
                return VehicleStatus switch
                {
                    UnifiedVehicleStatus.Submitted => "bg-warning text-dark",
                    UnifiedVehicleStatus.ApprovedByDealer => "bg-success",
                    UnifiedVehicleStatus.RejectedByDealer => "bg-danger",
                    _ => "bg-secondary"
                };
            }

            return Status switch
            {
                0 => "bg-warning text-dark",
                1 => "bg-success",
                2 => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public bool CanBeAllocated() => Status == 0;
    }
}

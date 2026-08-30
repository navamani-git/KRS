using System.ComponentModel.DataAnnotations.Schema;

namespace KRSDealerManagement.Domain.Entities
{    /// <summary>
    /// One requested vehicle line on a purchase order.
    /// Qty is expanded into one row per vehicle at order create time.
    /// </summary>
    public class PurchaseOrderItem
    {
        public int OrderItemId { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>0=Pending, 1=Approved, 2=Rejected</summary>
        public int Status { get; set; }

        public string? MotorNo { get; set; }
        public string? BatteryNo { get; set; }
        public string? ChargerNo { get; set; }
        public string? ControllerNo { get; set; }
        public string? ConverterNo { get; set; }
        public string? ChassisNumber { get; set; }
        public int? SubdealerVehicleId { get; set; }

        /// <summary>Backward-compatible alias for SubdealerVehicleId.</summary>
        [NotMapped]
        public int? VehicleId
        {
            get => SubdealerVehicleId;
            set => SubdealerVehicleId = value;
        }

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public bool IsPending() => Status == 0;
        public bool CanBeApproved() => Status == 0;
        public bool CanBeRejected() => Status == 0;
    }
}

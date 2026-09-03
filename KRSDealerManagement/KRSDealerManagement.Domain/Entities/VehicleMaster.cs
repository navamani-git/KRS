namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Physical OEM stock received from Ampere (dealer inventory).
    /// </summary>
    public class VehicleMaster
    {
        public int VehicleMasterId { get; set; }
        public int DealershipId { get; set; }
        public string ChassisNumber { get; set; } = "";
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public string MotorNo { get; set; } = "";
        public string BatteryNo { get; set; } = "";
        public string ChargerNo { get; set; } = "";
        public string ControllerNo { get; set; } = "";
        public string ConverterNo { get; set; } = "";
        public string AmpereInvoiceNo { get; set; } = "";
        public DateTime AmpereInvoiceDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public bool IsAllocated { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class VehicleMasterHistory
    {
        public int VehicleMasterHistoryId { get; set; }
        public int VehicleMasterId { get; set; }
        public string Action { get; set; } = "";
        public string? Remarks { get; set; }
        public string? DetailsJson { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class SubdealerVehicleHistory
    {
        public int SubdealerVehicleHistoryId { get; set; }
        public int SubdealerVehicleId { get; set; }
        public string Action { get; set; } = "";
        public string? Remarks { get; set; }
        public string? DetailsJson { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

namespace KRSDealerManagement.Application.DTOs
{
    public class VehicleMasterDto
    {
        public int VehicleMasterId { get; set; }
        public int DealershipId { get; set; }
        public string DealershipName { get; set; } = "";
        public string ChassisNumber { get; set; } = "";
        public int ModelId { get; set; }
        public string ModelName { get; set; } = "";
        public int ColorId { get; set; }
        public string ColorName { get; set; } = "";
        public string MotorNo { get; set; } = "";
        public string BatteryNo { get; set; } = "";
        public string ChargerNo { get; set; } = "";
        public string ControllerNo { get; set; } = "";
        public string ConverterNo { get; set; } = "";
        public string AmpereInvoiceNo { get; set; } = "";
        public DateTime AmpereInvoiceDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public bool IsAllocated { get; set; }
        public string? AllocatedToSubdealerName { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class VehicleMasterOptionDto
    {
        public int VehicleMasterId { get; set; }
        public string ChassisNumber { get; set; } = "";
        public string MotorNo { get; set; } = "";
        public string BatteryNo { get; set; } = "";
        public string ChargerNo { get; set; } = "";
        public string ControllerNo { get; set; } = "";
        public string ConverterNo { get; set; } = "";
        public string AmpereInvoiceNo { get; set; } = "";
    }
}

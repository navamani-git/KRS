using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class CreateVehicleMasterCommand : IRequest<int>
    {
        public int DealershipId { get; set; }
        public required string ChassisNumber { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public required string MotorNo { get; set; }
        public required string BatteryNo { get; set; }
        public required string ChargerNo { get; set; }
        public required string ControllerNo { get; set; }
        public required string ConverterNo { get; set; }
        public string AmpereInvoiceNo { get; set; } = "";
        public DateTime AmpereInvoiceDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateVehicleMasterCommand : IRequest<bool>
    {
        public int VehicleMasterId { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public required string MotorNo { get; set; }
        public required string BatteryNo { get; set; }
        public required string ChargerNo { get; set; }
        public required string ControllerNo { get; set; }
        public required string ConverterNo { get; set; }
        public string AmpereInvoiceNo { get; set; } = "";
        public DateTime AmpereInvoiceDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? Remarks { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class DeleteVehicleMasterCommand : IRequest<bool>
    {
        public int VehicleMasterId { get; set; }
        public int DeletedBy { get; set; }
        public string? Remarks { get; set; }
    }

    public class ImportVehicleMastersCommand : IRequest<ImportVehicleMastersResult>
    {
        public int DealershipId { get; set; }
        public int ImportedBy { get; set; }
        public required List<ImportVehicleMasterRow> Rows { get; set; }
    }

    public class ImportVehicleMasterRow
    {
        public int DealershipId { get; set; }
        public string ChassisNumber { get; set; } = "";
        public int? ModelId { get; set; }
        public int? ColorId { get; set; }
        public string? ModelName { get; set; }
        public string? ColorName { get; set; }
        public string MotorNo { get; set; } = "";
        public string BatteryNo { get; set; } = "";
        public string ChargerNo { get; set; } = "";
        public string ControllerNo { get; set; } = "";
        public string ConverterNo { get; set; } = "";
        public string AmpereInvoiceNo { get; set; } = "";
        public DateTime AmpereInvoiceDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class ImportVehicleMastersResult
    {
        public int ImportedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

    public class TransferVehicleMasterCommand : IRequest<bool>
    {
        public int VehicleMasterId { get; set; }
        public int TargetDealershipId { get; set; }
        public int TransferredBy { get; set; }
        public string? Remarks { get; set; }
    }
}

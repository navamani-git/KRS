using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Commands
{
    public class SaveWarrantyClaimCommand : IRequest<int>
    {
        public int? WarrantyClaimId { get; set; }
        public bool Submit { get; set; }
        public int UserId { get; set; }
        public int AccountId { get; set; }
        public int SubdealerId { get; set; }
        public int? DealershipId { get; set; }

        public string ClaimType { get; set; } = "";
        public int? SubdealerVehicleId { get; set; }
        public string ChassisNo { get; set; } = "";
        public string? CustomerName { get; set; }
        public string? CustomerMobile { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactMobile { get; set; }
        public int? ModelId { get; set; }
        public string? ModelName { get; set; }
        public int? ColorId { get; set; }
        public string? ColorName { get; set; }
        public int? CurrentKms { get; set; }
        public DateTime? SaleDate { get; set; }
        public DateTime? ComplaintDate { get; set; }
        public int? WarrantyPartId { get; set; }
        public string? OtherPartName { get; set; }
        public string? PartCode { get; set; }
        public string? FailurePartSerialNumber { get; set; }
        public string? CustomerComplaint { get; set; }
        public string? DealerObservation { get; set; }
        public string? Remarks { get; set; }
        public List<WarrantyServiceEntryInput> ServiceEntries { get; set; } = new();
        public Dictionary<string, string> AttachmentPaths { get; set; } = new();
    }

    public class WarrantyServiceEntryInput
    {
        public string ServiceType { get; set; } = "";
        public DateTime? ServiceDate { get; set; }
        public int? ServiceKms { get; set; }
        public int SortOrder { get; set; }
    }

    public class WarrantyClaimActionCommand : IRequest<bool>
    {
        public int WarrantyClaimId { get; set; }
        public int UserId { get; set; }
        public string? Notes { get; set; }
        public bool IsSystemAdmin { get; set; }
    }

    public class ApproveWarrantyClaimCommand : WarrantyClaimActionCommand { }
    public class RejectWarrantyClaimCommand : WarrantyClaimActionCommand { }
    public class RequestWarrantyInfoCommand : WarrantyClaimActionCommand { }
    public class ApplyWarrantyToAmpereCommand : WarrantyClaimActionCommand
    {
        public string SoNumber { get; set; } = "";
    }
    public class UpdateWarrantySoNumberCommand : WarrantyClaimActionCommand
    {
        public string SoNumber { get; set; } = "";
    }
    public class MarkWarrantyProductReceivedCommand : WarrantyClaimActionCommand { }
    public class MarkWarrantyCollectedCommand : WarrantyClaimActionCommand
    {
        public int AccountId { get; set; }
    }
    public class MarkWarrantyDefectiveSubmittedCommand : WarrantyClaimActionCommand
    {
        public int AccountId { get; set; }
    }
    public class MarkWarrantyDefectiveSentToAmpereCommand : WarrantyClaimActionCommand { }
}

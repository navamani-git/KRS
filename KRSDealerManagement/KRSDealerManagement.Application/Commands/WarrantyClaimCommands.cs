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
        public int? VehicleMasterId { get; set; }
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
        public DateTime? ActionDate { get; set; }
    }

    public class AcceptWarrantyClaimCommand : WarrantyClaimActionCommand { }

    [Obsolete("Use AcceptWarrantyClaimCommand")]
    public class ApproveWarrantyClaimCommand : AcceptWarrantyClaimCommand { }

    public class RejectWarrantyClaimCommand : WarrantyClaimActionCommand { }
    public class RequestWarrantyInfoCommand : WarrantyClaimActionCommand { }
    public class ApplyWarrantyToAmpereCommand : WarrantyClaimActionCommand { }
    public class MarkWarrantyAmpereApprovedCommand : WarrantyClaimActionCommand { }
    public class UpdateWarrantySoNumberCommand : WarrantyClaimActionCommand
    {
        public string SoNumber { get; set; } = "";
    }
    public class SaveWarrantyResolutionPartCommand : WarrantyClaimActionCommand
    {
        public string DealerResolutionType { get; set; } = "";
        public string DealerClosedPartNumber { get; set; } = "";
    }
    public class SaveWarrantyDealerInvoiceClosedCommand : WarrantyClaimActionCommand
    {
        public string DealerClosedInvoiceNumber { get; set; } = "";
    }
    public class MarkWarrantyReplacementPartReceivedCommand : WarrantyClaimActionCommand { }
    public class MarkWarrantySubdealerPartReceivedCommand : WarrantyClaimActionCommand
    {
        public int AccountId { get; set; }
        public string ReceivedByName { get; set; } = "";
        public bool OnBehalfOfSubdealerByStaff { get; set; }
    }
    public class MarkWarrantyDefectiveHandoverCommand : WarrantyClaimActionCommand
    {
        public int AccountId { get; set; }
        public string HandoverByName { get; set; } = "";
        public bool OnBehalfOfSubdealerByStaff { get; set; }
    }
    public class MarkWarrantyDefectiveSentToAmpereCommand : WarrantyClaimActionCommand { }
}

namespace KRSDealerManagement.Application.DTOs
{
    public class WarrantyClaimDto
    {
        public int WarrantyClaimId { get; set; }
        public string ClaimNumber { get; set; } = "";
        public string ClaimType { get; set; } = "";
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public int AccountId { get; set; }
        public string? AccountName { get; set; }
        public int SubdealerId { get; set; }
        public int? DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public string ChassisNo { get; set; } = "";
        public string? CustomerName { get; set; }
        public string? PartName { get; set; }
        public int? CurrentKms { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class WarrantyClaimDetailDto : WarrantyClaimDto
    {
        public int? SubdealerVehicleId { get; set; }
        public string? CustomerMobile { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactMobile { get; set; }
        public int? ModelId { get; set; }
        public string? ModelName { get; set; }
        public int? ColorId { get; set; }
        public string? ColorName { get; set; }
        public DateTime? SaleDate { get; set; }
        public DateTime? ComplaintDate { get; set; }
        public int? WarrantyPartId { get; set; }
        public string? OtherPartName { get; set; }
        public string? PartCode { get; set; }
        public string? FailurePartSerialNumber { get; set; }
        public string? CustomerComplaint { get; set; }
        public string? DealerObservation { get; set; }
        public string? Remarks { get; set; }
        public string? RejectionReason { get; set; }
        public string? MoreInfoNotes { get; set; }
        public string? SoNumber { get; set; }

        public DateTime? AmpereAppliedDate { get; set; }
        public string? AmpereAppliedByName { get; set; }
        public DateTime? ProductReceivedDate { get; set; }
        public string? ProductReceivedByName { get; set; }
        public DateTime? CollectedDate { get; set; }
        public string? CollectedByName { get; set; }
        public DateTime? DefectiveSubmittedDate { get; set; }
        public string? DefectiveSubmittedByName { get; set; }
        public DateTime? DefectiveSentToAmpereDate { get; set; }
        public string? DefectiveSentToAmpereByName { get; set; }

        public List<WarrantyClaimServiceEntryDto> ServiceEntries { get; set; } = new();
        public List<WarrantyClaimAttachmentDto> Attachments { get; set; } = new();
        public List<WarrantyClaimHistoryDto> History { get; set; } = new();
    }

    public class WarrantyClaimServiceEntryDto
    {
        public int ServiceEntryId { get; set; }
        public string ServiceType { get; set; } = "";
        public DateTime? ServiceDate { get; set; }
        public int? ServiceKms { get; set; }
        public int SortOrder { get; set; }
    }

    public class WarrantyClaimAttachmentDto
    {
        public int AttachmentId { get; set; }
        public string AttachmentType { get; set; } = "";
        public string AttachmentTypeName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string? OriginalFileName { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class WarrantyClaimHistoryDto
    {
        public int HistoryId { get; set; }
        public int? FromStatus { get; set; }
        public string? FromStatusName { get; set; }
        public int ToStatus { get; set; }
        public string? ToStatusName { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedDate { get; set; }
        public string? Notes { get; set; }
    }

    public class WarrantyChassisLookupDto
    {
        public int? VehicleId { get; set; }
        public string ChassisNo { get; set; } = "";
        public int? ModelId { get; set; }
        public string? ModelName { get; set; }
        public int? ColorId { get; set; }
        public string? ColorName { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerMobile { get; set; }
        public DateTime? SaleDate { get; set; }
        public bool IsKnownSoldVehicle { get; set; }
    }
}

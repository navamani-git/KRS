namespace KRSDealerManagement.Domain.Entities
{
    public class WarrantyPartMaster
    {
        public int WarrantyPartId { get; set; }
        public string PartName { get; set; } = "";
        public string? PartCode { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class WarrantyClaim
    {
        public int WarrantyClaimId { get; set; }
        public string ClaimNumber { get; set; } = "";
        public string ClaimType { get; set; } = "";
        public int Status { get; set; }

        public int AccountId { get; set; }
        public int SubdealerId { get; set; }
        public int? DealershipId { get; set; }
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

        public DateTime? SubmittedDate { get; set; }
        public int? SubmittedByUserId { get; set; }
        public string? RejectionReason { get; set; }
        public string? MoreInfoNotes { get; set; }
        public int? MoreInfoRequestedByUserId { get; set; }
        public DateTime? MoreInfoRequestedDate { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RejectedByUserId { get; set; }
        public DateTime? RejectedDate { get; set; }

        public string? SoNumber { get; set; }

        public int? AmpereAppliedByUserId { get; set; }
        public DateTime? AmpereAppliedDate { get; set; }
        public int? ProductReceivedByUserId { get; set; }
        public DateTime? ProductReceivedDate { get; set; }
        public int? CollectedByAccountId { get; set; }
        public DateTime? CollectedDate { get; set; }
        public int? DefectiveSubmittedByAccountId { get; set; }
        public DateTime? DefectiveSubmittedDate { get; set; }
        public int? DefectiveSentToAmpereByUserId { get; set; }
        public DateTime? DefectiveSentToAmpereDate { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class WarrantyClaimServiceEntry
    {
        public int ServiceEntryId { get; set; }
        public int WarrantyClaimId { get; set; }
        public string ServiceType { get; set; } = "";
        public DateTime? ServiceDate { get; set; }
        public int? ServiceKms { get; set; }
        public int SortOrder { get; set; }
    }

    public class WarrantyClaimAttachment
    {
        public int AttachmentId { get; set; }
        public int WarrantyClaimId { get; set; }
        public string AttachmentType { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string? OriginalFileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentType { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class WarrantyClaimStatusHistory
    {
        public int HistoryId { get; set; }
        public int WarrantyClaimId { get; set; }
        public int? FromStatus { get; set; }
        public int ToStatus { get; set; }
        public int ChangedByUserId { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}

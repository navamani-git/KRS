namespace KRSDealerManagement.Web.Models
{
    using KRSDealerManagement.Shared.Constants;

    public class WarrantyClaimFormModel
    {
        public int WarrantyClaimId { get; set; }
        public string ClaimType { get; set; } = WarrantyClaimTypes.Warranty;
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
        public string? PartCode { get; set; }
        public string? FailurePartSerialNumber { get; set; }
        public string? CustomerComplaint { get; set; }
        public string? DealerObservation { get; set; }
        public string? Remarks { get; set; }
        public List<WarrantyServiceEntryFormModel> ServiceEntries { get; set; } = new() { new(), new() };
        public List<WarrantyAttachmentUploadModel>? AttachmentFiles { get; set; }
    }

    public class WarrantyServiceEntryFormModel
    {
        public string ServiceType { get; set; } = "";
        public DateTime? ServiceDate { get; set; }
        public int? ServiceKms { get; set; }
    }

    public class WarrantyAttachmentUploadModel
    {
        public string AttachmentType { get; set; } = "";
        public IFormFile? File { get; set; }
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace KRSDealerManagement.Domain.Entities
{
    public class DocumentTypeMaster
    {
        public int DocumentTypeId { get; set; }
        public string TypeName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class RtoDistrictMaster
    {
        public int RtoDistrictId { get; set; }
        public string DistrictName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class RtoLocationMaster
    {
        public int RtoLocationId { get; set; }
        public int RtoDistrictId { get; set; }
        public string LocationName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

  public class VehicleBooking
    {
        public int VehicleBookingId { get; set; }
        [Column("SubdealerVehicleId")]
        public int VehicleId { get; set; }
        public int SubdealerId { get; set; }
        public int BookingStatus { get; set; } = 1;
        public string CustomerName { get; set; } = "";
        public bool IsCompanyBooking { get; set; }
        public string CustomerMobile { get; set; } = "";
        public string AlternativeMobile { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string EAadhaarPath { get; set; } = "";
        public string EAadhaarPassword { get; set; } = "";
        public int DocumentTypeId { get; set; }
        public string DocumentPath { get; set; } = "";
        public string? GstCertificatePath { get; set; }
        public string CustomerPhotoPath { get; set; } = "";
        public string ChassisPhotoPath { get; set; } = "";
        public string CustomerSignPath { get; set; } = "";
        public int RtoLocationId { get; set; }
        public bool FancyNumber { get; set; }
        public string PaymentMode { get; set; } = "";
        public int FinanceNameId { get; set; }
        public string NomineeName { get; set; } = "";
        public DateTime NomineeDob { get; set; }
        public string NomineeRelationship { get; set; } = "";
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime? PaperReceivedDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? InvoicePath { get; set; }
        public DateTime? InsuranceDate { get; set; }
        public string? InsurancePath { get; set; }
        public DateTime? AgentDate { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? RtoNumber { get; set; }
        public DateTime? NumberPlateReceivedDate { get; set; }
        public string? NumberPlateReceivedBy { get; set; }
        public string? SubsidyId { get; set; }
        public string? SubsidyCustomerNameCaps { get; set; }
        public string? FaceVerificationPath { get; set; }
        public string? RcImagePath { get; set; }
        public string? BoothPhotoPath { get; set; }
        public string? SubsidyUndertakingPath { get; set; }
        public DateTime? SubsidyDocsSubmittedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}

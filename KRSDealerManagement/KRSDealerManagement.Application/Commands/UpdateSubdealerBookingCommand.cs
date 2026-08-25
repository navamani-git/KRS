using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class UpdateSubdealerBookingCommand : IRequest<bool>
    {
        public int VehicleBookingId { get; set; }
        public int SubdealerId { get; set; }
        public required string CustomerName { get; set; }
        public bool IsCompanyBooking { get; set; }
        public required string CustomerMobile { get; set; }
        public required string AlternativeMobile { get; set; }
        public required string CustomerEmail { get; set; }
        public required string EAadhaarPassword { get; set; }
        public int DocumentTypeId { get; set; }
        public int RtoLocationId { get; set; }
        public bool FancyNumber { get; set; }
        public required string PaymentMode { get; set; }
        public int FinanceNameId { get; set; }
        public required string NomineeName { get; set; }
        public DateTime NomineeDob { get; set; }
        public required string NomineeRelationship { get; set; }
        public string? EditReason { get; set; }
        public string? EAadhaarPath { get; set; }
        public string? DocumentPath { get; set; }
        public string? GstCertificatePath { get; set; }
        public string? CustomerPhotoPath { get; set; }
        public string? ChassisPhotoPath { get; set; }
        public string? CustomerSignPath { get; set; }
        public int UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
    }
}

namespace KRSDealerManagement.Web.Models
{
    public class BookingFormInput
    {
        public string? CustomerName { get; set; }
        public bool IsCompanyBooking { get; set; }
        public string? CustomerMobile { get; set; }
        public string? AlternativeMobile { get; set; }
        public string? CustomerEmail { get; set; }
        public string? EAadhaarPassword { get; set; }
        public int? DocumentTypeId { get; set; }
        public int? RtoLocationId { get; set; }
        public int? RtoDistrictId { get; set; }
        public bool FancyNumber { get; set; }
        public string? PaymentMode { get; set; }
        public int? FinanceNameId { get; set; }
        public string? NomineeName { get; set; }
        public DateTime? NomineeDob { get; set; }
        public string? NomineeRelationship { get; set; }
        public string? EditReason { get; set; }
    }
}

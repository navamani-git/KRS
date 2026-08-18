namespace KRSDealerManagement.Application.DTOs
{
    public class CommissionPreviewRowDto
    {
        public int VehicleId { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public required string ChassisNumber { get; set; }
        public required string ModelName { get; set; }
        public required string ColorName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal? ApplicableRate { get; set; }
        public string CommissionStatus { get; set; } = "Not Submitted";
        public int? CommissionId { get; set; }
        public decimal? SubmittedAmount { get; set; }
        public bool CanSubmit =>
            !IsSubmitted && ApplicableRate.HasValue && ApplicableRate.Value > 0;

        public bool IsSubmitted => CommissionId.HasValue;
    }
}

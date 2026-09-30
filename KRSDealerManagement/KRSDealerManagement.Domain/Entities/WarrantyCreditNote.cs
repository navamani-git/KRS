namespace KRSDealerManagement.Domain.Entities
{
    public class WarrantyCreditNote
    {
        public int WarrantyClaimId { get; set; }
        public decimal UnitPrice { get; set; }
        public int UnitPriceGstRateId { get; set; }
        public decimal UnitPriceGstPercent { get; set; }
        public decimal HandlingCharges { get; set; }
        public int HandlingGstRateId { get; set; }
        public decimal HandlingGstPercent { get; set; }
        public decimal LabourCharges { get; set; }
        public int LabourGstRateId { get; set; }
        public decimal LabourGstPercent { get; set; }
        public decimal TotalWithoutGst { get; set; }
        public decimal TotalWithGst { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}

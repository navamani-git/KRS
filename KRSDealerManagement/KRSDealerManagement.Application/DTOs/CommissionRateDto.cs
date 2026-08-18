namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Commission Rate Data Transfer Object
    /// </summary>
    public class CommissionRateDto
    {
        public int CommissionRateId { get; set; }
        public int ModelId { get; set; }
        public required string ModelName { get; set; }
        public decimal CommissionAmount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public int StartMonth { get; set; }
        public int StartYear { get; set; }
        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetDisplayInfo()
            => $"₹{CommissionAmount:N2} ({EffectiveFrom:yyyy-MM-dd} to {EffectiveTo:yyyy-MM-dd})";

        public bool IsEffectiveForMonthYear(int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return EffectiveFrom.Date <= monthEnd && EffectiveTo.Date >= monthStart;
        }

        public bool IsActive()
            => IsEffectiveAsOf(DateTime.UtcNow);

        public bool IsEffectiveAsOf(DateTime asOfDate)
        {
            var asOf = asOfDate.Date;
            return EffectiveFrom.Date <= asOf && EffectiveTo.Date >= asOf;
        }
    }
}

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
        {
            string startDate = $"{StartYear}-{StartMonth:D2}";
            string endDate = ExpiryYear.HasValue && ExpiryMonth.HasValue
                ? $"{ExpiryYear}-{ExpiryMonth:D2}"
                : "Ongoing";

            return $"₹{CommissionAmount:N2} ({startDate} to {endDate})";
        }

        public bool IsEffectiveForMonthYear(int month, int year)
        {
            if (year < StartYear || (year == StartYear && month < StartMonth))
                return false;

            if (ExpiryYear.HasValue && ExpiryMonth.HasValue)
            {
                if (year > ExpiryYear || (year == ExpiryYear && month > ExpiryMonth))
                    return false;
            }

            return true;
        }

        public bool IsActive()
        {
            var now = DateTime.UtcNow;
            int currentMonth = now.Month;
            int currentYear = now.Year;

            return IsEffectiveForMonthYear(currentMonth, currentYear);
        }
    }
}

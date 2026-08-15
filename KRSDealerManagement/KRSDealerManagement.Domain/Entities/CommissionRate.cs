namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Commission rate configuration per vehicle model for period
    /// Defines how much commission is earned per vehicle per month
    /// </summary>
    public class CommissionRate
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int CommissionRateId { get; set; }

        /// <summary>
        /// Reference to VehicleModel
        /// Commission rates are model-based (not color-based)
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// Commission amount in rupees for this model
        /// This amount is earned per vehicle per month
        /// </summary>
        public decimal CommissionAmount { get; set; }

        /// <summary>
        /// Effective start month (1-12)
        /// </summary>
        public int StartMonth { get; set; }

        /// <summary>
        /// Effective start year
        /// </summary>
        public int StartYear { get; set; }

        /// <summary>
        /// Effective expiry month (1-12)
        /// If null, considered ongoing
        /// </summary>
        public int? ExpiryMonth { get; set; }

        /// <summary>
        /// Effective expiry year
        /// If null, considered ongoing
        /// </summary>
        public int? ExpiryYear { get; set; }

        /// <summary>
        /// Optional notes about rate change
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Admin who set this rate
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// When rate was created (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin who last modified rate
        /// </summary>
        public int? ModifiedBy { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if this rate is effective for given month/year
        /// </summary>
        public bool IsEffectiveForMonthYear(int month, int year)
        {
            // Check if rate starts before or at this month/year
            if (year < StartYear || (year == StartYear && month < StartMonth))
                return false;

            // Check if rate expires after this month/year (if expiry set)
            if (ExpiryYear.HasValue && ExpiryMonth.HasValue)
            {
                if (year > ExpiryYear || (year == ExpiryYear && month > ExpiryMonth))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get commission rate display info
        /// </summary>
        public string GetDisplayInfo()
        {
            string startDate = $"{StartYear}-{StartMonth:D2}";
            string endDate = ExpiryYear.HasValue && ExpiryMonth.HasValue
                ? $"{ExpiryYear}-{ExpiryMonth:D2}"
                : "Ongoing";

            return $"₹{CommissionAmount:N2} ({startDate} to {endDate})";
        }

        /// <summary>
        /// Check if rate is currently active
        /// </summary>
        public bool IsActive()
        {
            var now = DateTime.UtcNow;
            int currentMonth = now.Month;
            int currentYear = now.Year;

            return IsEffectiveForMonthYear(currentMonth, currentYear);
        }
    }
}

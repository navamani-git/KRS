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
        /// Effective start date — rate applies from this date (inclusive).
        /// Supports multiple rates within the same calendar month.
        /// </summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// Effective end date — rate applies through this date (inclusive).
        /// </summary>
        public DateTime EffectiveTo { get; set; }

        /// <summary>
        /// Effective start month (1-12) — derived from EffectiveFrom for legacy filters
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
            if (year < StartYear || (year == StartYear && month < StartMonth))
                return false;

            if (ExpiryYear.HasValue && ExpiryMonth.HasValue)
            {
                if (year > ExpiryYear || (year == ExpiryYear && month > ExpiryMonth))
                    return false;
            }

            return true;
        }

        public bool IsEffectiveAsOf(DateTime asOfDate)
        {
            var asOf = asOfDate.Date;
            return EffectiveFrom.Date <= asOf && EffectiveTo.Date >= asOf;
        }

        /// <summary>
        /// Get commission rate display info
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"₹{CommissionAmount:N2} ({EffectiveFrom:yyyy-MM-dd} to {EffectiveTo:yyyy-MM-dd})";
        }

        /// <summary>
        /// Check if rate is currently active
        /// </summary>
        public bool IsActive()
        {
            return IsEffectiveAsOf(DateTime.UtcNow);
        }
    }
}

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Tracks vehicle price history by month
    /// Allows price changes monthly with historical tracking
    /// </summary>
    public class VehiclePriceHistory
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int PriceHistoryId { get; set; }

        /// <summary>
        /// Optional reference to a physical Vehicle (null for catalogue model+color pricing)
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// Vehicle model this price applies to
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// Vehicle color this price applies to
        /// </summary>
        public int ColorId { get; set; }

        /// <summary>
        /// Month (1-12)
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Year (e.g., 2024)
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Price for this month in rupees
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Date from which this catalogue price is effective (multiple entries allowed per month)
        /// </summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// Optional notes about price change
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Admin who set/modified this price
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// When price was set (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin who last modified price
        /// </summary>
        public int? ModifiedBy { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get display string for this price record
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"{Year}-{Month:D2}: ₹{Price:N2}";
        }

        /// <summary>
        /// Check if this is price for specified month/year
        /// </summary>
        public bool IsForMonthYear(int month, int year)
        {
            return Month == month && Year == year;
        }
    }
}

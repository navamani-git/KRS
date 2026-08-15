namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a vehicle model (e.g., BMW 3 Series, Toyota Innova)
    /// </summary>
    public class VehicleModel
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// Model name (must be unique)
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Optional model description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether model is active/available for purchase
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Admin user who created this model
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Model creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin user who last modified this model
        /// </summary>
        public int? ModifiedBy { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if model can be purchased
        /// </summary>
        public bool IsAvailableForPurchase()
        {
            return IsActive;
        }
    }
}

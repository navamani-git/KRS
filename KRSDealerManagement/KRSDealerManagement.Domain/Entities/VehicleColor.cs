namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a vehicle color variant
    /// </summary>
    public class VehicleColor
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int ColorId { get; set; }

        /// <summary>
        /// Color name (must be unique)
        /// </summary>
        public string ColorName { get; set; }

        /// <summary>
        /// Hex code for color display (e.g., "#FFFFFF" for white)
        /// </summary>
        public string HexCode { get; set; }

        /// <summary>
        /// Whether color is available for selection
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Admin user who created this color
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// Color creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin user who last modified this color
        /// </summary>
        public int? ModifiedBy { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if color can be selected
        /// </summary>
        public bool IsAvailable()
        {
            return IsActive;
        }

        /// <summary>
        /// Get color display with hex code
        /// </summary>
        public string GetColorDisplay()
        {
            return string.IsNullOrEmpty(HexCode) 
                ? ColorName 
                : $"{ColorName} ({HexCode})";
        }
    }
}

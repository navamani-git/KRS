namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Maps which global colors are available for a specific vehicle model.
    /// </summary>
    public class VehicleModelColor
    {
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}

namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Vehicle Model Data Transfer Object
    /// </summary>
    public class VehicleModelDto
    {
        public int ModelId { get; set; }
        public required string ModelName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}

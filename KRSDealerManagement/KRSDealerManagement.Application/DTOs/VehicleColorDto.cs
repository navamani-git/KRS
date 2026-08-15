namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Vehicle Color Data Transfer Object
    /// </summary>
    public class VehicleColorDto
    {
        public int ColorId { get; set; }
        public required string ColorName { get; set; }
        public string? HexCode { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetColorDisplay()
        {
            return string.IsNullOrEmpty(HexCode)
                ? ColorName
                : $"{ColorName} ({HexCode})";
        }
    }
}

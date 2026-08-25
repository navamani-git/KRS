namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Vehicle Price History Data Transfer Object
    /// </summary>
    public class VehiclePriceHistoryDto
    {
        public int PriceHistoryId { get; set; }
        public int VehicleId { get; set; }
        public int ModelId { get; set; }
        public required string ModelName { get; set; }
        public int ColorId { get; set; }
        public required string ColorName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetDisplayInfo()
        {
            return $"{Year}-{Month:D2}: ₹{Price:N2}";
        }

        public bool IsForMonthYear(int month, int year)
        {
            return Month == month && Year == year;
        }
    }
}

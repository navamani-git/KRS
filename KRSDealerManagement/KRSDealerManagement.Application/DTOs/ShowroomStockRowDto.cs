namespace KRSDealerManagement.Application.DTOs
{
    public class ShowroomStockRowDto
    {
        public int VehicleId { get; set; }
        public required string ChassisNumber { get; set; }
        public required string ModelName { get; set; }
        public required string ColorName { get; set; }
        public int SubdealerId { get; set; }
        public required string SubdealerName { get; set; }
        public string? DealershipLocation { get; set; }
        public string? DealershipName { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime? AllocatedDate { get; set; }
        public decimal CurrentPrice { get; set; }
        public int DaysInStock { get; set; }
    }
}

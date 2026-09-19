namespace KRSDealerManagement.Application.DTOs
{
    public class WarrantyOnlyVehicleEditDto
    {
        public int VehicleMasterId { get; set; }
        public string ChassisNumber { get; set; } = "";
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public string? Remarks { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerMobile { get; set; }
        public DateTime? SaleDate { get; set; }
    }
}

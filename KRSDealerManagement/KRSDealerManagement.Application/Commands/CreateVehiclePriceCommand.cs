using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create vehicle price for specific month/year
    /// Will automatically log to AuditLog
    /// </summary>
    public class CreateVehiclePriceCommand : IRequest<int>
    {
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public int Month { get; set; } // 1-12
        public int Year { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}

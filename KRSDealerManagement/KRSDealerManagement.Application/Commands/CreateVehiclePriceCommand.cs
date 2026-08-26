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
        /// <summary>Selected colors from the price entry form (one or many).</summary>
        public List<int> ColorIds { get; set; } = new();
        public bool ApplyForAllColors { get; set; }
        public int Month { get; set; } // 1-12
        public int Year { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}

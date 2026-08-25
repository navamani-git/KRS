using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Update vehicle price
    /// Will log old and new prices to AuditLog
    /// </summary>
    public class UpdateVehiclePriceCommand : IRequest<bool>
    {
        public int PriceHistoryId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
        public int ModifiedBy { get; set; }
        public string? Remarks { get; set; }
    }
}

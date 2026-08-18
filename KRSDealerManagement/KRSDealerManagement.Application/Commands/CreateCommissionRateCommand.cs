using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create commission rate for vehicle model
    /// Will log to AuditLog
    /// </summary>
    public class CreateCommissionRateCommand : IRequest<int>
    {
        public int ModelId { get; set; }
        public decimal CommissionAmount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}

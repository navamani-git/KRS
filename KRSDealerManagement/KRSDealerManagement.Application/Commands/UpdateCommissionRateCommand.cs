using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class UpdateCommissionRateCommand : IRequest<bool>
    {
        public int CommissionRateId { get; set; }
        public decimal CommissionAmount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public string? Notes { get; set; }
        public int ModifiedBy { get; set; }
    }
}

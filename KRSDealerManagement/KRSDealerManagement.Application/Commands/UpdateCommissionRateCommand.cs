using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Update commission rate
    /// Will log changes to AuditLog
    /// </summary>
    public class UpdateCommissionRateCommand : IRequest<bool>
    {
        public int CommissionRateId { get; set; }
        public decimal CommissionAmount { get; set; }
        public int? ExpiryMonth { get; set; }
        public int? ExpiryYear { get; set; }
        public string? Notes { get; set; }
        public int ModifiedBy { get; set; }
        public required string Remarks { get; set; }
    }
}

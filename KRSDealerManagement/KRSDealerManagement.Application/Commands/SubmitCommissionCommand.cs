using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Submit commission for vehicle in specific month
    /// Will log to AuditLog
    /// </summary>
    public class SubmitCommissionCommand : IRequest<int>
    {
        public int SubdealerId { get; set; }
        public required string ChassisNumber { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal CommissionAmount { get; set; }
        public string? Notes { get; set; }
        public int SubmittedBy { get; set; }
    }
}

using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create return request for vehicle
    /// Will log to AuditLog
    /// </summary>
    public class CreateReturnRequestCommand : IRequest<int>
    {
        public int AccountId { get; set; }
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public required string ReturnReason { get; set; }
        public int CreatedBy { get; set; }
    }
}

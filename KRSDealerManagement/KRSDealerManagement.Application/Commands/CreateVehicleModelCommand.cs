using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create new vehicle model command
    /// Will automatically log to AuditLog
    /// </summary>
    public class CreateVehicleModelCommand : IRequest<int>
    {
        public required string ModelName { get; set; }
        public required string Description { get; set; }
        public int CreatedBy { get; set; } // UserId from context
        public List<int> ColorIds { get; set; } = new();
    }
}

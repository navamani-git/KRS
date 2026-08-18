using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Update vehicle model command
    /// Will automatically log changes to AuditLog
    /// </summary>
    public class UpdateVehicleModelCommand : IRequest<bool>
    {
        public int ModelId { get; set; }
        public required string ModelName { get; set; }
        public required string Description { get; set; }
        public bool IsActive { get; set; }
        public int ModifiedBy { get; set; } // UserId from context
        public required string Remarks { get; set; } // For audit trail
        public List<int> ColorIds { get; set; } = new();
    }
}

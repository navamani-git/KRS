using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    /// <summary>
    /// Create new vehicle color command
    /// </summary>
    public class CreateVehicleColorCommand : IRequest<int>
    {
        public required string ColorName { get; set; }
        public string? HexCode { get; set; }
        public int CreatedBy { get; set; }
    }

    /// <summary>
    /// Update vehicle color command
    /// </summary>
    public class UpdateVehicleColorCommand : IRequest<bool>
    {
        public int ColorId { get; set; }
        public required string ColorName { get; set; }
        public string? HexCode { get; set; }
        public bool IsActive { get; set; }
        public int ModifiedBy { get; set; }
        public string? Remarks { get; set; }
    }
}

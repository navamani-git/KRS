using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class AdminDeleteVehicleCommand : IRequest<bool>
    {
        public int VehicleId { get; set; }
        public required string DeleteReason { get; set; }
        public int DeletedBy { get; set; }
        public required string DeletedByName { get; set; }
    }
}

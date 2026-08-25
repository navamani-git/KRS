using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetVehicleChassisHistoryQuery : IRequest<VehicleChassisHistoryDto?>
    {
        public required string ChassisNumber { get; set; }
    }
}

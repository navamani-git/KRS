using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get specific vehicle model by ID
    /// </summary>
    public class GetVehicleModelByIdQuery : IRequest<VehicleModelDto>
    {
        public int ModelId { get; set; }
    }
}

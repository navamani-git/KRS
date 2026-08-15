using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get all vehicle colors with optional filtering
    /// </summary>
    public class GetVehicleColorsQuery : IRequest<IEnumerable<VehicleColorDto>>
    {
        public bool? IsActive { get; set; }
        public string SearchTerm { get; set; }
    }
}

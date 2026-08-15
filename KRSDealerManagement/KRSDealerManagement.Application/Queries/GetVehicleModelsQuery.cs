using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get all vehicle models with optional filtering
    /// </summary>
    public class GetVehicleModelsQuery : IRequest<IEnumerable<VehicleModelDto>>
    {
        public bool? IsActive { get; set; } // Filter by active status
        public string SearchTerm { get; set; } // Search by model name
    }
}

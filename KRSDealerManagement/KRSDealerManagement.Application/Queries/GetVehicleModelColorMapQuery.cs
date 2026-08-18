using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Returns active model-to-color mappings for cascading dropdowns.
    /// Key = ModelId, Value = mapped colors for that model.
    /// </summary>
    public class GetVehicleModelColorMapQuery : IRequest<Dictionary<int, List<VehicleColorDto>>>
    {
        public bool ActiveModelsOnly { get; set; } = true;
        public bool ActiveColorsOnly { get; set; } = true;
    }
}

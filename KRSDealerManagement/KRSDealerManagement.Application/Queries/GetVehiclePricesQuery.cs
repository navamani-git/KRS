using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get vehicle prices with filtering by model, color, and month/year
    /// </summary>
    public class GetVehiclePricesQuery : IRequest<IEnumerable<VehiclePriceHistoryDto>>
    {
        public int? ModelId { get; set; }
        public int? ColorId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetVehiclesQuery : IRequest<IEnumerable<VehicleDto>>
    {
        public int? SubdealerId { get; set; }
        public int? DealershipId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public Dictionary<string, string>? ColumnFilters { get; set; }
    }
}

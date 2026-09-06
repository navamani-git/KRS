using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetVehicleAgingQuery : IRequest<IEnumerable<VehicleAgingRowDto>>
    {
        public int? DealershipId { get; set; }
        public string? DealershipLocation { get; set; }
        public int? SubdealerId { get; set; }
        public string? SearchTerm { get; set; }
    }
}

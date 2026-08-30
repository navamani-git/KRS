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
        public string? DealershipLocation { get; set; }
        public Dictionary<string, string>? ColumnFilters { get; set; }
        /// <summary>When true, only dealer-rejected vehicles (subdealer rejected view).</summary>
        public bool RejectedOnly { get; set; }
        /// <summary>Hide rejected vehicles from the main list (subdealer My Vehicles).</summary>
        public bool ExcludeRejected { get; set; }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetShowroomStockQuery : IRequest<IEnumerable<ShowroomStockRowDto>>
    {
        /// <summary>Limit to subdealers under this dealership (branch manager scope).</summary>
        public int? DealershipId { get; set; }
        public string? DealershipLocation { get; set; }
        public int? SubdealerId { get; set; }
        public string? SearchTerm { get; set; }
    }
}

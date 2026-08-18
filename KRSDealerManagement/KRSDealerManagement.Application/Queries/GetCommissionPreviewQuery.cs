using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetCommissionPreviewQuery : IRequest<IEnumerable<CommissionPreviewRowDto>>
    {
        public int SubdealerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        /// <summary>When true, only vehicles with no commission submitted for invoice month.</summary>
        public bool PendingOnly { get; set; }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetSubdealerDetailQuery : IRequest<SubdealerDetailDto?>
    {
        /// <summary>SubDealers.SubDealerId (preferred).</summary>
        public int? SubDealerId { get; set; }

        /// <summary>Legacy: resolve org via login user id.</summary>
        public int? UserId { get; set; }

        public int? DealershipId { get; set; }
    }
}

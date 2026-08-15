using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetSubdealerDetailQuery : IRequest<SubdealerDetailDto?>
    {
        public int UserId { get; set; }
        public int? DealershipId { get; set; }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get accounts for specific subdealer with filtering
    /// </summary>
    public class GetSubdealerAccountsQuery : IRequest<IEnumerable<SubdealerAccountDto>>
    {
        public int SubdealerId { get; set; }
        public bool? IsActive { get; set; }
    }
}

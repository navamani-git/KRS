using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get commission rates with filtering
    /// </summary>
    public class GetCommissionRatesQuery : IRequest<IEnumerable<CommissionRateDto>>
    {
        public int? ModelId { get; set; }
        public bool? ActiveOnly { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}

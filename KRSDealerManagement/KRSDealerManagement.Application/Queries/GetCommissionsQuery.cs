using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get commissions with advanced filtering
    /// </summary>
    public class GetCommissionsQuery : IRequest<IEnumerable<CommissionDto>>
    {
        public int? SubdealerId { get; set; }
        public int? AccountId { get; set; }
        public int? Status { get; set; } // 0=Pending, 1=Approved, 2=Paid, 3=Rejected
        public int? Month { get; set; }
        public int? Year { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

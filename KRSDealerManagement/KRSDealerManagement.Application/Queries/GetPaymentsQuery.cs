using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get payments with filtering
    /// </summary>
    public class GetPaymentsQuery : IRequest<IEnumerable<PaymentDto>>
    {
        public int? SubdealerId { get; set; }
        public int? AccountId { get; set; }
        public int? Status { get; set; } // 0=Pending, 1=Approved, 2=Rejected
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? AppliedOnly { get; set; }
    }
}

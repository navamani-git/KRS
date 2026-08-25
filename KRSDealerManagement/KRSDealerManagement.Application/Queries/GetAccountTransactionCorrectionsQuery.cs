using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetAccountTransactionCorrectionsQuery : IRequest<IEnumerable<AccountTransactionCorrectionDto>>
    {
        public int? AccountId { get; set; }
        public int? TransactionId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

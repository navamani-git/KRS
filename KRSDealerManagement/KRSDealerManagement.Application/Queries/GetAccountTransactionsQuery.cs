using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get account transaction history with filtering
    /// </summary>
    public class GetAccountTransactionsQuery : IRequest<IEnumerable<AccountTransactionDto>>
    {
        public int AccountId { get; set; }
        public int? TransactionType { get; set; } // 1=Debit, 2=Credit
        public string ReferenceType { get; set; } // Filter by reference type
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        /// <summary>When true, reserved/released hold rows are omitted from the statement.</summary>
        public bool ExcludeBalanceHolds { get; set; } = true;
        /// <summary>When true, includes admin soft-deleted rows (admin screens only).</summary>
        public bool IncludeDeleted { get; set; }
    }
}

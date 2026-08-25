using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Detailed transaction history for each account
    /// Records every debit/credit operation for audit trail
    /// </summary>
    public class AccountTransaction
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Reference to SubdealerAccount
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Transaction type: Debit, Credit
        /// </summary>
        public int TransactionType { get; set; } // 1=Debit, 2=Credit

        /// <summary>
        /// Amount involved in transaction
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Balance after this transaction
        /// Used for balance history tracking
        /// </summary>
        public decimal BalanceAfterTransaction { get; set; }

        /// <summary>
        /// Reason/description of transaction
        /// e.g., "Purchase Order Approval", "Commission Credit", "Return Refund"
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Reference ID of related entity
        /// e.g., OrderId for purchase, CommissionId for commission, etc.
        /// </summary>
        public int? ReferenceId { get; set; }

        /// <summary>
        /// Type of reference: PurchaseOrder, Commission, Return, Payment, Adjustment, etc.
        /// </summary>
        public string ReferenceType { get; set; }

        /// <summary>
        /// Admin remarks on transaction
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// User who initiated transaction (admin/dealer/system)
        /// </summary>
        public int InitiatedBy { get; set; }

        /// <summary>
        /// Transaction timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Soft-deleted by admin; hidden from subdealer statement, kept for admin audit.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Get transaction type as display text
        /// </summary>
        public string GetTransactionTypeDisplay()
            => AccountTransactionTypeHelper.GetDisplayName(TransactionType);

        public string GetTransactionSign()
            => IsDebit() ? "-" : "+";

        public string GetSignedAmount()
            => $"{GetTransactionSign()}₹{Amount:N2}";

        public string GetDisplayInfo()
            => $"{GetTransactionTypeDisplay()} | {GetSignedAmount()} | Balance: ₹{BalanceAfterTransaction:N2}";

        public bool IsDebit()
            => AccountTransactionTypeHelper.IsDebit(TransactionType);

        public bool IsCredit()
            => AccountTransactionTypeHelper.IsCredit(TransactionType);
    }
}

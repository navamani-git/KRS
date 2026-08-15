using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Account Transaction Data Transfer Object
    /// </summary>
    public class AccountTransactionDto
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public int TransactionType { get; set; } // 1=Debit, 2=Credit
        public decimal Amount { get; set; }
        public decimal BalanceAfterTransaction { get; set; }
        public required string Reason { get; set; }
        public int? ReferenceId { get; set; }
        public required string ReferenceType { get; set; }
        public string CategoryLabel { get; set; } = "";
        public string? ChassisNumber { get; set; }
        public string? Remarks { get; set; }
        public int InitiatedBy { get; set; }
        public required string InitiatedByName { get; set; }
        public DateTime CreatedDate { get; set; }

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

        public bool IsBalanceHold()
            => AccountTransactionTypeHelper.IsBalanceHold(TransactionType);
    }
}

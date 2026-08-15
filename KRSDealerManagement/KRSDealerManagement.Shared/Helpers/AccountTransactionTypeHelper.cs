using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Shared.Helpers
{
    public static class AccountTransactionTypeHelper
    {
        public static bool IsDebit(int transactionType) => transactionType switch
        {
            1 => true,
            5 => true,
            _ => false
        };

        public static bool IsCredit(int transactionType) => transactionType switch
        {
            2 => true,
            6 => true,
            7 => true,
            8 => true,
            _ => false
        };

        /// <summary>Reserved (3) or released (4) — holds on available balance, not a debit/credit.</summary>
        public static bool IsBalanceHold(int transactionType) => transactionType is 3 or 4;

        public static string GetDisplayName(int transactionType)
        {
            return transactionType switch
            {
                1 => "Debit",
                2 => "Credit",
                3 => "Reserved",
                4 => "Released",
                7 => "Commission Credit",
                8 => "Commission Rejected",
                _ => Enum.IsDefined(typeof(TransactionTypeEnum), transactionType)
                    ? ((TransactionTypeEnum)transactionType).GetDisplayName()
                    : "Transaction"
            };
        }

        public static decimal EstimateBalanceBefore(int transactionType, decimal amount, decimal balanceAfter)
        {
            if (IsDebit(transactionType))
                return balanceAfter + amount;
            if (IsCredit(transactionType))
                return balanceAfter - amount;
            return balanceAfter;
        }
    }
}

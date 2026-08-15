namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Account balance tracking per SubdealerAccount
    /// Maintains current balance, reserved amount for pending orders, and transaction history
    /// </summary>
    public class AccountBalance
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int BalanceId { get; set; }

        /// <summary>
        /// Reference to SubdealerAccount this balance belongs to
        /// Each account has exactly one balance
        /// </summary>
        public int SubdealerAccountId { get; set; }

        /// <summary>
        /// Subdealer user ID (denormalized for quick reference)
        /// </summary>
        public int SubdealerId { get; set; }

        /// <summary>
        /// Current account balance
        /// Can be positive, zero, or negative (for admin accounts)
        /// </summary>
        public decimal CurrentBalance { get; set; } = 0;

        /// <summary>
        /// Amount reserved for pending purchase orders
        /// Blocked from new purchases until order is approved/rejected
        /// </summary>
        public decimal ReservedAmount { get; set; } = 0;

        /// <summary>
        /// Available balance = CurrentBalance - ReservedAmount
        /// What account can actually use for new purchases
        /// Calculated property - denormalized for performance
        /// </summary>
        public decimal AvailableBalance { get; set; } = 0;

        /// <summary>
        /// Initial balance set by admin when account created
        /// Used for reporting and audit purposes
        /// </summary>
        public decimal? InitialBalance { get; set; }

        /// <summary>
        /// Timestamp of last transaction on this account
        /// Used for activity tracking
        /// </summary>
        public DateTime? LastTransactionDate { get; set; }

        /// <summary>
        /// Account creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Recalculate available balance
        /// Call this after changing CurrentBalance or ReservedAmount
        /// </summary>
        public void RecalculateAvailableBalance()
        {
            AvailableBalance = CurrentBalance - ReservedAmount;
        }

        /// <summary>
        /// Check if account has sufficient available balance for transaction
        /// </summary>
        public bool HasSufficientBalance(decimal amount)
        {
            return AvailableBalance >= amount;
        }

        /// <summary>
        /// Reserve amount for pending transaction
        /// Blocks amount from being used for new purchases
        /// </summary>
        public void ReserveAmount(decimal amount)
        {
            ReservedAmount += amount;
            RecalculateAvailableBalance();
        }

        /// <summary>
        /// Release previously reserved amount
        /// </summary>
        public void ReleaseReservedAmount(decimal amount)
        {
            ReservedAmount = Math.Max(0, ReservedAmount - amount);
            RecalculateAvailableBalance();
        }

        /// <summary>
        /// Debit amount from current balance
        /// Called when transaction is approved
        /// </summary>
        public void Debit(decimal amount)
        {
            CurrentBalance -= amount;
            RecalculateAvailableBalance();
            LastTransactionDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Credit amount to current balance
        /// Called when transaction is approved or commission received
        /// </summary>
        public void Credit(decimal amount)
        {
            CurrentBalance += amount;
            RecalculateAvailableBalance();
            LastTransactionDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Get balance summary as formatted string
        /// </summary>
        public string GetBalanceSummary()
        {
            return $"Current: ₹{CurrentBalance:N2} | Reserved: ₹{ReservedAmount:N2} | Available: ₹{AvailableBalance:N2}";
        }
    }
}

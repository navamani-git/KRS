namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Account Balance Data Transfer Object
    /// </summary>
    public class AccountBalanceDto
    {
        public int BalanceId { get; set; }
        public int SubdealerAccountId { get; set; }
        public int SubdealerId { get; set; }
        public required string SubdealerName { get; set; }
        public required string AccountName { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal ReservedAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal? InitialBalance { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetBalanceSummary()
        {
            return $"Current: ₹{CurrentBalance:N2} | Reserved: ₹{ReservedAmount:N2} | Available: ₹{AvailableBalance:N2}";
        }

        public bool HasSufficientBalance(decimal amount)
        {
            return AvailableBalance >= amount;
        }
    }
}

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Admin-only audit trail for account transaction edits and deletes.
    /// Not shown on subdealer statement.
    /// </summary>
    public class AccountTransactionCorrection
    {
        public int CorrectionId { get; set; }
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public string Action { get; set; } = "";
        public string OldSnapshot { get; set; } = "";
        public string? NewSnapshot { get; set; }
        public string CorrectionReason { get; set; } = "";
        public int CorrectedBy { get; set; }
        public string? CorrectedByName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

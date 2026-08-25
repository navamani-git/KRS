namespace KRSDealerManagement.Application.DTOs
{
    public class AccountTransactionCorrectionDto
    {
        public int CorrectionId { get; set; }
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public string? SubdealerName { get; set; }
        public string Action { get; set; } = "";
        public string OldSnapshot { get; set; } = "";
        public string? NewSnapshot { get; set; }
        public string CorrectionReason { get; set; } = "";
        public int CorrectedBy { get; set; }
        public string? CorrectedByName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

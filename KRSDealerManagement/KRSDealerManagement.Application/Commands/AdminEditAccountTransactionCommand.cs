using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class AdminEditAccountTransactionCommand : IRequest<bool>
    {
        public int TransactionId { get; set; }
        public int TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Reason { get; set; } = "";
        public string? Remarks { get; set; }
        public decimal? RequestedAmount { get; set; }
        public decimal? ApprovedPaymentAmount { get; set; }
        public DateTime? PaymentSubmittedDate { get; set; }
        public DateTime? PaymentApprovedDate { get; set; }
        public DateTime? PaymentReceivedDate { get; set; }
        public string? CustomerName { get; set; }
        public int? PaymentTypeId { get; set; }
        public int? FinanceNameId { get; set; }
        public string? VinNumber { get; set; }
        public decimal? CommissionAmount { get; set; }
        public string CorrectionReason { get; set; } = "";
        public int CorrectedBy { get; set; }
        public string? CorrectedByName { get; set; }
    }
}

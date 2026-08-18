using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class AdminCorrectPaymentCommand : IRequest<bool>
    {
        public int PaymentId { get; set; }
        /// <summary>Requested amount submitted by subdealer.</summary>
        public decimal Amount { get; set; }
        public decimal? ActualReceivedAmount { get; set; }
        public DateTime? ActualReceivedDate { get; set; }
        public int PaymentTypeId { get; set; }
        public DateTime PaymentDate { get; set; }
        public int Status { get; set; }
        public string? CustomerName { get; set; }
        public int? FinanceNameId { get; set; }
        public string? VinNumber { get; set; }
        public string? SubdealerRemarks { get; set; }
        public required string CorrectionReason { get; set; }
        public int CorrectedBy { get; set; }
        public required string CorrectedByName { get; set; }
    }
}

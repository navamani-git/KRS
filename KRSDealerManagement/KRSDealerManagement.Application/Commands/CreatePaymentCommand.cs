using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class CreatePaymentCommand : IRequest<int>
    {
        public int AccountId { get; set; }
        public int SubdealerId { get; set; }
        public decimal Amount { get; set; }
        public int PaymentTypeId { get; set; }
        public required string PaymentType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? SubdealerRemarks { get; set; }
        public string? OtherPaymentType { get; set; }
        public string? CustomerName { get; set; }
        public int? FinanceNameId { get; set; }
        public string? VinNumber { get; set; }
        public string? CreditRequestModelName { get; set; }
        public string? CreditRequestColorName { get; set; }
        public string? PaymentProofPath { get; set; }
        public string? PaymentProof2Path { get; set; }
        public bool RequiresFinanceDetails { get; set; }
        public bool IsCreditRequest { get; set; }
        public int CreatedBy { get; set; }
    }
}

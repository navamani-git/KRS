using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
    {
        public CreatePaymentCommandValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0);
            RuleFor(x => x.SubdealerId).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
            RuleFor(x => x.PaymentTypeId).GreaterThan(0).WithMessage("Payment type is required");
            RuleFor(x => x.PaymentType).NotEmpty();
            RuleFor(x => x.PaymentDate).NotEmpty();

            RuleFor(x => x.PaymentProofPath)
                .NotEmpty().When(x => !x.IsCreditRequest)
                .WithMessage("Payment proof is required");

            RuleFor(x => x.CustomerName)
                .NotEmpty().When(x => !x.IsCreditRequest && !x.ExemptCustomerName)
                .WithMessage("Customer name is required for this payment type")
                .Matches("^[A-Z0-9 ]+$").When(x => !x.IsCreditRequest && !x.ExemptCustomerName && !string.IsNullOrWhiteSpace(x.CustomerName))
                .WithMessage("Customer name must be CAPS only");

            RuleFor(x => x.FinanceNameId)
                .GreaterThan(0).When(x => x.RequiresFinanceDetails && !x.IsCreditRequest)
                .WithMessage("Finance name is required for Finance payments");

            RuleFor(x => x.VinNumber)
                .NotEmpty().When(x => x.RequiresFinanceDetails && !x.IsCreditRequest)
                .WithMessage("VIN / Chassis number is required for Finance payments");

            RuleFor(x => x.CreatedBy).GreaterThan(0);
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AdminCorrectPaymentCommandValidator : AbstractValidator<AdminCorrectPaymentCommand>
    {
        public AdminCorrectPaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.ActualReceivedAmount)
                .GreaterThan(0).When(x => x.Status == 1)
                .WithMessage("Actual received amount is required for approved payments");
            RuleFor(x => x.ActualReceivedDate)
                .NotNull().When(x => x.Status == 1)
                .WithMessage("Actual received date is required for approved payments");
            RuleFor(x => x.PaymentTypeId).GreaterThan(0);
            RuleFor(x => x.CorrectionReason).NotEmpty().MinimumLength(5);
            RuleFor(x => x.CorrectedBy).GreaterThan(0);
            RuleFor(x => x.CorrectedByName).NotEmpty();
        }
    }
}

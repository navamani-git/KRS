using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class RejectPaymentCommandValidator : AbstractValidator<RejectPaymentCommand>
    {
        public RejectPaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0).WithMessage("Valid payment ID is required");

            RuleFor(x => x.RejectedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

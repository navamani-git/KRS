using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class ApprovePaymentCommandValidator : AbstractValidator<ApprovePaymentCommand>
    {
        public ApprovePaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0).WithMessage("Valid payment ID is required");

            RuleFor(x => x.ApprovedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

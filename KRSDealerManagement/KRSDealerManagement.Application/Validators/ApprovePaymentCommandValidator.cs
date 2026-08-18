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

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Approval remarks are required");

            RuleFor(x => x.ActualReceivedDate)
                .NotEmpty().WithMessage("Actual received date is required");

            RuleFor(x => x.ActualReceivedAmount)
                .GreaterThan(0).When(x => x.ActualReceivedAmount.HasValue)
                .WithMessage("Actual received amount must be greater than zero");
        }
    }
}

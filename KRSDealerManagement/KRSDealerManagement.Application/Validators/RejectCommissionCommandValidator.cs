using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class RejectCommissionCommandValidator : AbstractValidator<RejectCommissionCommand>
    {
        public RejectCommissionCommandValidator()
        {
            RuleFor(x => x.CommissionId)
                .GreaterThan(0).WithMessage("Valid commission ID is required");

            RuleFor(x => x.RejectedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Rejection reason is required");
        }
    }
}

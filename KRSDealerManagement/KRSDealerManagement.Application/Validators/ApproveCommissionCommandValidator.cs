using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class ApproveCommissionCommandValidator : AbstractValidator<ApproveCommissionCommand>
    {
        public ApproveCommissionCommandValidator()
        {
            RuleFor(x => x.CommissionId)
                .GreaterThan(0).WithMessage("Valid commission ID is required");

            RuleFor(x => x.ApprovedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

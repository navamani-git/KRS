using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class UpdateCommissionRateCommandValidator : AbstractValidator<UpdateCommissionRateCommand>
    {
        public UpdateCommissionRateCommandValidator()
        {
            RuleFor(x => x.CommissionRateId)
                .GreaterThan(0).WithMessage("Valid commission rate ID is required");

            RuleFor(x => x.CommissionAmount)
                .GreaterThan(0).WithMessage("Commission amount must be greater than 0");

            RuleFor(x => x.ModifiedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

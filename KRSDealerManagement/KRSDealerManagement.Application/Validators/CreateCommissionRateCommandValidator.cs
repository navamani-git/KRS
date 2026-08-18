using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateCommissionRateCommandValidator : AbstractValidator<CreateCommissionRateCommand>
    {
        public CreateCommissionRateCommandValidator()
        {
            RuleFor(x => x.ModelId)
                .GreaterThan(0).WithMessage("Valid model ID is required");

            RuleFor(x => x.CommissionAmount)
                .GreaterThan(0).WithMessage("Commission amount must be greater than 0");

            RuleFor(x => x.EffectiveFrom)
                .NotEmpty().WithMessage("Effective from date is required");

            RuleFor(x => x.EffectiveTo)
                .NotEmpty().WithMessage("Effective to date is required");

            RuleFor(x => x.EffectiveTo)
                .GreaterThanOrEqualTo(x => x.EffectiveFrom.Date)
                .WithMessage("Effective to must be on or after effective from");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

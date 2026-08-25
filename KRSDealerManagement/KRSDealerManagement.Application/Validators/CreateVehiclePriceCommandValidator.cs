using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateVehiclePriceCommandValidator : AbstractValidator<CreateVehiclePriceCommand>
    {
        public CreateVehiclePriceCommandValidator()
        {
            RuleFor(x => x.ModelId)
                .GreaterThan(0).WithMessage("Valid model ID is required");

            RuleFor(x => x.ColorId)
                .GreaterThan(0).WithMessage("Valid color ID is required")
                .When(x => !x.ApplyForAllColors);

            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12");

            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(2020).WithMessage("Year must be valid");

            RuleFor(x => x.EffectiveTo)
                .Must((cmd, to) => to == default || to.Date >= (cmd.EffectiveFrom == default
                    ? new DateTime(cmd.Year, cmd.Month, 1)
                    : cmd.EffectiveFrom.Date))
                .WithMessage("Effective to must be on or after effective from");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class UpdateVehiclePriceCommandValidator : AbstractValidator<UpdateVehiclePriceCommand>
    {
        public UpdateVehiclePriceCommandValidator()
        {
            RuleFor(x => x.PriceHistoryId)
                .GreaterThan(0).WithMessage("Valid price history ID is required");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.EffectiveTo)
                .Must((cmd, to) => to.Date >= cmd.EffectiveFrom.Date)
                .WithMessage("Effective to must be on or after effective from");

            RuleFor(x => x.ModifiedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

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
                .GreaterThan(0).WithMessage("Valid color ID is required");

            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12");

            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(DateTime.Now.Year).WithMessage("Year must be current or future");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

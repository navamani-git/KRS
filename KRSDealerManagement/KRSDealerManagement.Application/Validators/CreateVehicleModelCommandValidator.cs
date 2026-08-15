using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateVehicleModelCommandValidator : AbstractValidator<CreateVehicleModelCommand>
    {
        public CreateVehicleModelCommandValidator()
        {
            RuleFor(x => x.ModelName)
                .NotEmpty().WithMessage("Model name is required")
                .Length(3, 100).WithMessage("Model name must be between 3 and 100 characters");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

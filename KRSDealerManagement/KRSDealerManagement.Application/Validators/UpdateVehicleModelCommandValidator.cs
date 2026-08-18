using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class UpdateVehicleModelCommandValidator : AbstractValidator<UpdateVehicleModelCommand>
    {
        public UpdateVehicleModelCommandValidator()
        {
            RuleFor(x => x.ModelId)
                .GreaterThan(0).WithMessage("Valid model ID is required");

            RuleFor(x => x.ModelName)
                .NotEmpty().WithMessage("Model name is required")
                .Length(3, 100).WithMessage("Model name must be between 3 and 100 characters");

            RuleFor(x => x.ModifiedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required for audit trail");

            RuleFor(x => x.ColorIds)
                .NotEmpty().WithMessage("Select at least one color for this model.");
        }
    }
}

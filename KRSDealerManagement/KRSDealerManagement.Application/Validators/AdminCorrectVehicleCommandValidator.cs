using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AdminCorrectVehicleCommandValidator : AbstractValidator<AdminCorrectVehicleCommand>
    {
        public AdminCorrectVehicleCommandValidator()
        {
            RuleFor(x => x.VehicleId).GreaterThan(0);
            RuleFor(x => x.ModelId).GreaterThan(0);
            RuleFor(x => x.ColorId).GreaterThan(0);
            RuleFor(x => x.ChassisNumber).NotEmpty();
            RuleFor(x => x.CorrectionReason).NotEmpty().MinimumLength(5);
            RuleFor(x => x.CorrectedBy).GreaterThan(0);
            RuleFor(x => x.CorrectedByName).NotEmpty();
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AdminDeleteVehicleCommandValidator : AbstractValidator<AdminDeleteVehicleCommand>
    {
        public AdminDeleteVehicleCommandValidator()
        {
            RuleFor(x => x.VehicleId).GreaterThan(0);
            RuleFor(x => x.DeleteReason).NotEmpty().MinimumLength(5);
            RuleFor(x => x.DeletedBy).GreaterThan(0);
            RuleFor(x => x.DeletedByName).NotEmpty();
        }
    }
}

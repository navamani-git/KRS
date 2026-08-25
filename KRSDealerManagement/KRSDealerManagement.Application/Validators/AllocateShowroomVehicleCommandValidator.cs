using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AllocateShowroomVehicleCommandValidator : AbstractValidator<AllocateShowroomVehicleCommand>
    {
        public AllocateShowroomVehicleCommandValidator()
        {
            RuleFor(x => x.VehicleId).GreaterThan(0);
            RuleFor(x => x.SubdealerId).GreaterThan(0);
            RuleFor(x => x.AllocatedBy).GreaterThan(0);
            RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
        }
    }
}

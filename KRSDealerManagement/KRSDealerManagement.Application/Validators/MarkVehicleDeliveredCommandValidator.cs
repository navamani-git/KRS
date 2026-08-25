using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class MarkVehicleDeliveredCommandValidator : AbstractValidator<MarkVehicleDeliveredCommand>
    {
        public MarkVehicleDeliveredCommandValidator()
        {
            RuleFor(x => x.VehicleId).GreaterThan(0);
            RuleFor(x => x.MarkedBy).GreaterThan(0);
            RuleFor(x => x.DeliveryDate).NotEmpty();
        }
    }
}

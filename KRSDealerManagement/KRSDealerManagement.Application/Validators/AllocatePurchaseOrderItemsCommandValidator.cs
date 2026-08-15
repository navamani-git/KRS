using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AllocatePurchaseOrderItemsCommandValidator : AbstractValidator<AllocatePurchaseOrderItemsCommand>
    {
        public AllocatePurchaseOrderItemsCommandValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0);
            RuleFor(x => x.ApprovedBy).GreaterThan(0);
            RuleFor(x => x.Items).NotEmpty().WithMessage("Select at least one line item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                // OrderItemId is identity; allow 0 if DB ever seeded/reseeds to 0
                item.RuleFor(i => i.OrderItemId).GreaterThanOrEqualTo(0);
                item.RuleFor(i => i.ChassisNumber).NotEmpty().When(i => i.Approve)
                    .WithMessage("Chassis Number is required for approval");
                item.RuleFor(i => i.MotorNo).NotEmpty().When(i => i.Approve)
                    .WithMessage("Motor No is required for approval");
                item.RuleFor(i => i.BatteryNo).NotEmpty().When(i => i.Approve)
                    .WithMessage("Battery No is required for approval");
                item.RuleFor(i => i.ChargerNo).NotEmpty().When(i => i.Approve)
                    .WithMessage("Charger No is required for approval");
                item.RuleFor(i => i.ControllerNo).NotEmpty().When(i => i.Approve)
                    .WithMessage("Controller No is required for approval");
                item.RuleFor(i => i.ConverterNo).NotEmpty().When(i => i.Approve)
                    .WithMessage("Converter No is required for approval");
            });
        }
    }

    public class RejectPurchaseOrderItemsCommandValidator : AbstractValidator<RejectPurchaseOrderItemsCommand>
    {
        public RejectPurchaseOrderItemsCommandValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0);
            RuleFor(x => x.RejectedBy).GreaterThan(0);
            RuleFor(x => x.Remarks).NotEmpty();
        }
    }
}

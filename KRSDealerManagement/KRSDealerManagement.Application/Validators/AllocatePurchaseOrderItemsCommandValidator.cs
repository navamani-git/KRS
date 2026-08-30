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
                item.RuleFor(i => i.OrderItemId).GreaterThanOrEqualTo(0);
                item.RuleFor(i => i.VehicleMasterId).NotNull().GreaterThan(0).When(i => i.Approve)
                    .WithMessage("Select a chassis from dealer stock for approval");
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

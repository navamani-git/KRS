using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class ApprovePurchaseOrderItemCommandValidator : AbstractValidator<ApprovePurchaseOrderItemCommand>
    {
        public ApprovePurchaseOrderItemCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0).WithMessage("Valid order ID is required");

            // VehicleId is optional: Orders/Index approves/rejects the whole order (not per chassis)
            RuleFor(x => x.VehicleId)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0");

            RuleFor(x => x.ApprovedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required");
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
    {
        public CreatePurchaseOrderCommandValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage("Valid account ID is required");

            RuleFor(x => x.SubdealerId)
                .GreaterThan(0).WithMessage("Valid subdealer ID is required");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one order item is required");

            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemValidator());

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;
using static KRSDealerManagement.Application.Commands.CreatePurchaseOrderCommand;

namespace KRSDealerManagement.Application.Validators
{
    /// <summary>
    /// Nested validator for OrderItem within CreatePurchaseOrderCommand
    /// </summary>
    public class OrderItemValidator : AbstractValidator<OrderItem>
    {
        public OrderItemValidator()
        {
            RuleFor(x => x.ModelId)
                .GreaterThan(0).WithMessage("Valid model ID is required");

            RuleFor(x => x.ColorId)
                .GreaterThan(0).WithMessage("Valid color ID is required");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be greater than 0");
        }
    }
}

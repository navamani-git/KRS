using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateReturnRequestCommandValidator : AbstractValidator<CreateReturnRequestCommand>
    {
        public CreateReturnRequestCommandValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage("Valid account ID is required");

            RuleFor(x => x.OrderId)
                .GreaterThan(0).WithMessage("Valid order ID is required");

            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("Valid vehicle ID is required");

            RuleFor(x => x.ReturnReason)
                .NotEmpty().WithMessage("Return reason is required");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

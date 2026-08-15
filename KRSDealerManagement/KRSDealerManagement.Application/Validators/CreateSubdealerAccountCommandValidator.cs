using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateSubdealerAccountCommandValidator : AbstractValidator<CreateSubdealerAccountCommand>
    {
        public CreateSubdealerAccountCommandValidator()
        {
            RuleFor(x => x.SubdealerId)
                .GreaterThan(0).WithMessage("Valid subdealer ID is required");

            RuleFor(x => x.AccountName)
                .NotEmpty().WithMessage("Account name is required")
                .Length(3, 100).WithMessage("Account name must be between 3 and 100 characters");

            RuleFor(x => x.AccountType)
                .NotEmpty().WithMessage("Account type is required")
                .Length(3, 50).WithMessage("Account type must be between 3 and 50 characters");

            RuleFor(x => x.InitialBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Initial balance cannot be negative");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateSubdealerLoginCommandValidator : AbstractValidator<CreateSubdealerLoginCommand>
    {
        public CreateSubdealerLoginCommandValidator()
        {
            RuleFor(x => x.SubDealerId)
                .GreaterThan(0).WithMessage("Subdealer is required");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(50).WithMessage("Username must be at most 50 characters")
                .Matches(@"^[a-z0-9_\.]+$").WithMessage("Username may only contain lowercase letters, numbers, underscore and dot");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");

            RuleFor(x => x.InitialBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Initial balance cannot be negative");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

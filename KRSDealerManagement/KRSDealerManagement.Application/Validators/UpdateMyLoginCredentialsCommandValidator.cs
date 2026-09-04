using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class UpdateMyLoginCredentialsCommandValidator : AbstractValidator<UpdateMyLoginCredentialsCommand>
    {
        public UpdateMyLoginCredentialsCommandValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(50).WithMessage("Username must be at most 50 characters.")
                .Matches("^[a-z0-9_.]+$")
                .WithMessage("Username must be lowercase letters, numbers, underscore, or dot.");
            RuleFor(x => x.NewPassword)
                .MinimumLength(6).When(x => !string.IsNullOrWhiteSpace(x.NewPassword))
                .WithMessage("New password must be at least 6 characters.");
        }
    }
}

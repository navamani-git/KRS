using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class UpdateSubdealerLoginUsernameCommandValidator : AbstractValidator<UpdateSubdealerLoginUsernameCommand>
    {
        public UpdateSubdealerLoginUsernameCommandValidator()
        {
            RuleFor(x => x.LoginUserId).GreaterThan(0);
            RuleFor(x => x.SubDealerId).GreaterThan(0);
            RuleFor(x => x.UpdatedBy).GreaterThan(0);
            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50)
                .Matches("^[a-z0-9_.]+$")
                .WithMessage("Username must be lowercase letters, numbers, underscore, or dot.");
        }
    }
}

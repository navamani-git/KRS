using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateStaffUserCommandValidator : AbstractValidator<CreateStaffUserCommand>
    {
        public CreateStaffUserCommandValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MinimumLength(2);
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x => x.StaffRole).Must(r => r is 3 or 4).WithMessage("Role must be Finance Admin or Branch Manager.");
            RuleFor(x => x.DealershipId).GreaterThan(0);
            RuleFor(x => x.CreatedBy).GreaterThan(0);
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class ConfigureAccountPermissionsCommandValidator : AbstractValidator<ConfigureAccountPermissionsCommand>
    {
        public ConfigureAccountPermissionsCommandValidator()
        {
            RuleFor(x => x.AccountId)
                .GreaterThan(0).WithMessage("Valid account ID is required");

            RuleFor(x => x.Permissions)
                .NotNull().WithMessage("Permissions are required");

            RuleForEach(x => x.Permissions)
                .SetValidator(new PermissionSettingValidator());

            RuleFor(x => x.ConfiguredBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

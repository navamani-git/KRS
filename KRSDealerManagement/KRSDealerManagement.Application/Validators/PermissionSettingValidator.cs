using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class PermissionSettingValidator : AbstractValidator<PermissionSetting>
    {
        public PermissionSettingValidator()
        {
            RuleFor(x => x.MenuKey)
                .NotEmpty().WithMessage("Menu key is required");

            RuleFor(x => x.MenuName)
                .NotEmpty().WithMessage("Menu name is required");
        }
    }
}

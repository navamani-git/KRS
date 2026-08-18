using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class UpdateCommissionRateCommandValidator : AbstractValidator<UpdateCommissionRateCommand>
    {
        public UpdateCommissionRateCommandValidator()
        {
            RuleFor(x => x.CommissionRateId).GreaterThan(0);
            RuleFor(x => x.CommissionAmount).GreaterThan(0);
            RuleFor(x => x.EffectiveFrom).NotEmpty();
            RuleFor(x => x.EffectiveTo).NotEmpty();
            RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom.Date);
            RuleFor(x => x.ModifiedBy).GreaterThan(0);
        }
    }
}

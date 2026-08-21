using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AdjustSubdealerAccountCommandValidator : AbstractValidator<AdjustSubdealerAccountCommand>
    {
        public AdjustSubdealerAccountCommandValidator()
        {
            RuleFor(x => x.SubdealerId).GreaterThan(0);
            RuleFor(x => x.AdjustedBy).GreaterThan(0);
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.AdjustmentType)
                .NotEmpty()
                .Must(t => t.Equals("Credit", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Type must be Credit or Debit.");
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Remarks).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Remarks));
        }
    }
}

using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateCommissionRateCommandValidator : AbstractValidator<CreateCommissionRateCommand>
    {
        public CreateCommissionRateCommandValidator()
        {
            RuleFor(x => x.ModelId)
                .GreaterThan(0).WithMessage("Valid model ID is required");

            RuleFor(x => x.CommissionAmount)
                .GreaterThan(0).WithMessage("Commission amount must be greater than 0");

            RuleFor(x => x.StartMonth)
                .InclusiveBetween(1, 12).WithMessage("Start month must be between 1 and 12");

            RuleFor(x => x.StartYear)
                .GreaterThanOrEqualTo(DateTime.Now.Year).WithMessage("Start year must be current or future");

            RuleFor(x => x.ExpiryMonth)
                .InclusiveBetween(1, 12).When(x => x.ExpiryMonth.HasValue)
                .WithMessage("Expiry month must be between 1 and 12");

            RuleFor(x => x.ExpiryYear)
                .GreaterThanOrEqualTo(x => x.StartYear).When(x => x.ExpiryYear.HasValue)
                .WithMessage("Expiry year must be greater than or equal to start year");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

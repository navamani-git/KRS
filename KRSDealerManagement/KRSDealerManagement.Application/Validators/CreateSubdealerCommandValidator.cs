using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateSubdealerCommandValidator : AbstractValidator<CreateSubdealerCommand>
    {
        public CreateSubdealerCommandValidator()
        {
            RuleFor(x => x.SubdealerName)
                .NotEmpty().WithMessage("Subdealer name is required")
                .Length(3, 100).WithMessage("Subdealer name must be between 3 and 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email must be valid");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required")
                .Length(3, 100).WithMessage("Location must be between 3 and 100 characters");

            RuleFor(x => x.PrimaryPhone)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^\d{10}$").WithMessage("Phone number must be 10 digits");

            RuleFor(x => x.DealershipId)
                .GreaterThan(0).WithMessage("Dealership location is required");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

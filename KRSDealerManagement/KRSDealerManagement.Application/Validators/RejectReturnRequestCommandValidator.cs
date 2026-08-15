using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class RejectReturnRequestCommandValidator : AbstractValidator<RejectReturnRequestCommand>
    {
        public RejectReturnRequestCommandValidator()
        {
            RuleFor(x => x.ReturnRequestId)
                .GreaterThan(0).WithMessage("Valid return request ID is required");

            RuleFor(x => x.RejectedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");
        }
    }
}

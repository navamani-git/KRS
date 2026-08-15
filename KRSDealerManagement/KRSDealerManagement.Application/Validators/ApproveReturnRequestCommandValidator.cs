using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class ApproveReturnRequestCommandValidator : AbstractValidator<ApproveReturnRequestCommand>
    {
        public ApproveReturnRequestCommandValidator()
        {
            RuleFor(x => x.ReturnRequestId)
                .GreaterThan(0).WithMessage("Valid return request ID is required");

            RuleFor(x => x.ApprovedBy)
                .GreaterThan(0).WithMessage("Valid user ID is required");

            RuleFor(x => x.RefundAmount)
                .GreaterThan(0).WithMessage("Refund amount must be greater than 0");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required");

            RuleFor(x => x.ReassignToSubdealerId)
                .GreaterThan(0).When(x => x.ReassignToSubdealerId.HasValue)
                .WithMessage("Valid subdealer is required for reassignment");
        }
    }
}

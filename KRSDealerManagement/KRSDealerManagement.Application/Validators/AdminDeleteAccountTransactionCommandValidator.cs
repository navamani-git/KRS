using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class AdminDeleteAccountTransactionCommandValidator : AbstractValidator<AdminDeleteAccountTransactionCommand>
    {
        public AdminDeleteAccountTransactionCommandValidator()
        {
            RuleFor(x => x.TransactionId).GreaterThan(0);
            RuleFor(x => x.DeleteReason).NotEmpty().MinimumLength(5).MaximumLength(500);
            RuleFor(x => x.DeletedBy).GreaterThan(0);
        }
    }
}

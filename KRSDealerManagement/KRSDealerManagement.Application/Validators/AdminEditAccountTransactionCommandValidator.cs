using FluentValidation;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Validators
{
    public class AdminEditAccountTransactionCommandValidator : AbstractValidator<AdminEditAccountTransactionCommand>
    {
        public AdminEditAccountTransactionCommandValidator()
        {
            RuleFor(x => x.TransactionId).GreaterThan(0);
            RuleFor(x => x.TransactionType).Must(t =>
                AccountTransactionTypeHelper.IsDebit(t)
                || AccountTransactionTypeHelper.IsCredit(t)
                || AccountTransactionTypeHelper.IsBalanceHold(t))
                .WithMessage("Invalid transaction type.");
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
            RuleFor(x => x.CorrectionReason).NotEmpty().MinimumLength(5).MaximumLength(500);
            RuleFor(x => x.CorrectedBy).GreaterThan(0);
        }
    }
}

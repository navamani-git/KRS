using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Helpers
{
    public static class AccountTransactionBalanceRecalcHelper
    {
        public static decimal ApplyToRunningBalance(decimal runningBalance, AccountTransaction transaction)
        {
            if (AccountTransactionTypeHelper.IsDebit(transaction.TransactionType))
                return runningBalance - transaction.Amount;
            if (AccountTransactionTypeHelper.IsCredit(transaction.TransactionType))
                return runningBalance + transaction.Amount;
            return runningBalance;
        }

        public static async Task RecalculateAccountAsync(IUnitOfWork unitOfWork, int accountId)
        {
            var active = (await unitOfWork.AccountTransactions.GetAllAsync())
                .Where(t => t.AccountId == accountId && !t.IsDeleted)
                .OrderBy(t => t.CreatedDate)
                .ThenBy(t => t.TransactionId)
                .ToList();

            var balances = await unitOfWork.AccountBalances.GetAllAsync();
            var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == accountId);
            if (balance == null)
                return;

            // Opening balance is stored on AccountBalance; not all accounts have an AccountCreation txn row.
            decimal running = balance.InitialBalance ?? 0m;
            if (running == 0m)
            {
                var openingCredit = active.FirstOrDefault(t =>
                    string.Equals(t.ReferenceType, "AccountCreation", StringComparison.OrdinalIgnoreCase)
                    && AccountTransactionTypeHelper.IsCredit(t.TransactionType));
                if (openingCredit != null)
                    running = openingCredit.Amount;
            }

            foreach (var txn in active)
            {
                running = ApplyToRunningBalance(running, txn);
                if (txn.BalanceAfterTransaction != running)
                {
                    txn.BalanceAfterTransaction = running;
                    await unitOfWork.AccountTransactions.UpdateAsync(txn);
                }
            }

            balance.CurrentBalance = running;
            balance.RecalculateAvailableBalance();
            balance.LastTransactionDate = active.LastOrDefault()?.CreatedDate ?? balance.LastTransactionDate;
            balance.ModifiedDate = DateTime.UtcNow;
            await unitOfWork.AccountBalances.UpdateAsync(balance);
        }
    }
}

using System.Text.Json;
using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Helpers
{
    public static class AccountTransactionSnapshotHelper
    {
        public static string Serialize(AccountTransaction transaction, object? linked = null)
        {
            return JsonSerializer.Serialize(new
            {
                transaction.TransactionId,
                transaction.AccountId,
                transaction.TransactionType,
                transaction.Amount,
                transaction.BalanceAfterTransaction,
                transaction.Reason,
                transaction.ReferenceType,
                transaction.ReferenceId,
                transaction.Remarks,
                transaction.InitiatedBy,
                transaction.CreatedDate,
                transaction.IsDeleted,
                Linked = linked
            });
        }
    }
}

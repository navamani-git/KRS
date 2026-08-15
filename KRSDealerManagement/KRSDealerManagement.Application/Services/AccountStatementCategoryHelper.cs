using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Services
{
    public static class AccountStatementCategoryHelper
    {
        public static string Resolve(
            int transactionType,
            string? referenceType,
            int? referenceId,
            string? reason,
            IReadOnlyDictionary<int, Payment> payments,
            IReadOnlyDictionary<int, PaymentType> paymentTypes)
        {
            if (string.Equals(referenceType, "Payment", StringComparison.OrdinalIgnoreCase)
                && referenceId.HasValue
                && payments.TryGetValue(referenceId.Value, out var payment)
                && AccountTransactionTypeHelper.IsCredit(transactionType))
            {
                if (payment.PaymentTypeId.HasValue
                    && paymentTypes.TryGetValue(payment.PaymentTypeId.Value, out var paymentType))
                    return $"{paymentType.TypeName} Credited";

                if (!string.IsNullOrWhiteSpace(payment.PaymentType))
                    return $"{payment.PaymentType} Credited";

                return "Payment Credited";
            }

            if (string.Equals(referenceType, "ReturnRequest", StringComparison.OrdinalIgnoreCase)
                && AccountTransactionTypeHelper.IsCredit(transactionType))
                return "Return Credited";

            if (string.Equals(referenceType, "PurchaseOrder", StringComparison.OrdinalIgnoreCase)
                && AccountTransactionTypeHelper.IsDebit(transactionType))
                return "Order Debit";

            if (string.Equals(referenceType, "Commission", StringComparison.OrdinalIgnoreCase))
            {
                if (transactionType == 7) return "Commission Credited";
                if (transactionType == 8) return "Commission Rejected";
            }

            if (string.Equals(referenceType, "Vehicle", StringComparison.OrdinalIgnoreCase))
            {
                if (AccountTransactionTypeHelper.IsDebit(transactionType)) return "Price Adjustment Debit";
                if (AccountTransactionTypeHelper.IsCredit(transactionType)) return "Price Adjustment Credit";
            }

            if (!string.IsNullOrWhiteSpace(reason)
                && (reason.Contains("Initial", StringComparison.OrdinalIgnoreCase)
                    || reason.Contains("Opening", StringComparison.OrdinalIgnoreCase)))
                return "Opening Balance";

            if (AccountTransactionTypeHelper.IsDebit(transactionType)) return "Debit";
            if (AccountTransactionTypeHelper.IsCredit(transactionType)) return "Credit";

            return AccountTransactionTypeHelper.GetDisplayName(transactionType);
        }
    }
}

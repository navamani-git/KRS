using System.Text.RegularExpressions;
using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Services
{
    public static class PaymentStatementResolver
    {
        private static readonly Regex PaymentIdFromReason = new(
            @"Payment\s*#(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Payment? Resolve(
            AccountTransaction transaction,
            IReadOnlyDictionary<int, Payment> byPaymentId,
            IReadOnlyDictionary<int, Payment> byTransactionId)
        {
            if (string.Equals(transaction.ReferenceType, "Payment", StringComparison.OrdinalIgnoreCase)
                && transaction.ReferenceId.HasValue
                && byPaymentId.TryGetValue(transaction.ReferenceId.Value, out var byReference))
                return byReference;

            if (byTransactionId.TryGetValue(transaction.TransactionId, out var byTxn))
                return byTxn;

            var paymentId = TryParsePaymentId(transaction.Reason);
            if (paymentId.HasValue && byPaymentId.TryGetValue(paymentId.Value, out var byReason))
                return byReason;

            return null;
        }

        public static int? TryParsePaymentId(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return null;

            var match = PaymentIdFromReason.Match(reason);
            return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }
    }

}

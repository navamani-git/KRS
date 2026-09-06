using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Services
{
    public static class AccountStatementDateResolver
    {
        public static (DateTime StatementDate, List<StatementDateDetail> DetailDates) Resolve(
            AccountTransaction transaction,
            string referenceType,
            int? referenceId,
            Payment? payment,
            ReturnRequest? returnRequest,
            Commission? commission,
            PurchaseOrder? purchaseOrder,
            Vehicle? vehicle)
        {
            var details = new List<StatementDateDetail>();

            if (payment != null && AccountTransactionTypeHelper.IsCredit(transaction.TransactionType))
            {
                AddDateTime(details, "Submitted Date", payment.CreatedDate);
                AddDateTime(details, "Approved Date", payment.ProcessedDate);
                AddDate(details, "Received Date", payment.ActualReceivedDate);
                AddDateTime(details, "Payment Date", payment.PaymentDate);

                var statementDate = payment.ActualReceivedDate?.Date
                    ?? payment.ProcessedDate
                    ?? transaction.CreatedDate;
                AddDate(details, "Credit/Debit Date", statementDate);
                return (statementDate, details);
            }

            if (string.Equals(referenceType, "ReturnRequest", StringComparison.OrdinalIgnoreCase)
                && returnRequest != null)
            {
                AddDateTime(details, "Return Request Date", returnRequest.CreatedDate);
                AddDateTime(details, "Return Approved Date", returnRequest.ProcessedDate);
                var statementDate = returnRequest.ProcessedDate ?? transaction.CreatedDate;
                AddDate(details, "Credit/Debit Date", statementDate);
                return (statementDate, details);
            }

            if (string.Equals(referenceType, "Commission", StringComparison.OrdinalIgnoreCase)
                && commission != null)
            {
                AddDateTime(details, "Submitted Date", commission.CreatedDate);
                AddDateTime(details, "Approved Date", commission.ApprovedDate);
                AddDateTime(details, "Paid Date", commission.PaidDate);
                AddDateTime(details, "Rejected Date", commission.RejectedDate);

                var statementDate = commission.PaidDate
                    ?? commission.ApprovedDate
                    ?? commission.RejectedDate
                    ?? transaction.CreatedDate;
                AddDate(details, "Credit/Debit Date", statementDate);
                return (statementDate, details);
            }

            if (string.Equals(referenceType, "PurchaseOrder", StringComparison.OrdinalIgnoreCase)
                && purchaseOrder != null)
            {
                AddDateTime(details, "Order Submitted Date", purchaseOrder.CreatedDate);
                AddDateTime(details, "Order Approved Date", purchaseOrder.ApprovedDate);
                var statementDate = purchaseOrder.ApprovedDate ?? transaction.CreatedDate;
                AddDate(details, "Credit/Debit Date", statementDate);
                return (statementDate, details);
            }

            if (string.Equals(referenceType, "Vehicle", StringComparison.OrdinalIgnoreCase)
                && vehicle != null)
            {
                AddDateTime(details, "Allocated Date", vehicle.AllocatedDate);
                if (purchaseOrder != null)
                {
                    AddDateTime(details, "Order Submitted Date", purchaseOrder.CreatedDate);
                    AddDateTime(details, "Order Approved Date", purchaseOrder.ApprovedDate);
                }

                var statementDate = vehicle.AllocatedDate
                    ?? purchaseOrder?.ApprovedDate
                    ?? transaction.CreatedDate;
                AddDate(details, "Credit/Debit Date", statementDate);
                return (statementDate, details);
            }

            if (string.Equals(referenceType, "ManualAdjustment", StringComparison.OrdinalIgnoreCase))
            {
                AddDate(details, "Credit/Debit Date", transaction.CreatedDate);
                return (transaction.CreatedDate, details);
            }

            AddDate(details, "Credit/Debit Date", transaction.CreatedDate);
            return (transaction.CreatedDate, details);
        }

        private static void AddDateTime(List<StatementDateDetail> details, string label, DateTime? value)
        {
            if (!value.HasValue)
                return;

            details.Add(new StatementDateDetail
            {
                Label = label,
                Value = value.Value,
                DateOnly = false
            });
        }

        private static void AddDate(List<StatementDateDetail> details, string label, DateTime? value)
        {
            if (!value.HasValue)
                return;

            details.Add(new StatementDateDetail
            {
                Label = label,
                Value = value.Value,
                DateOnly = true
            });
        }
    }
}

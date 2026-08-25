using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Helpers
{
    public static class AccountStatementExportHelper
    {
        public static readonly string[] Headers =
        {
            "#", "Txn Date", "Type", "Description", "Customer", "Pay Type", "Finance", "VIN",
            "Requested Amt", "Approved Amt", "Debit", "Credit", "Balance",
            "Submitted Date", "Approved Date", "Received Date", "Remarks"
        };

        public static IEnumerable<IReadOnlyList<object?>> BuildRows(IEnumerable<AccountTransactionDto> transactions)
        {
            var sr = 1;
            foreach (var t in transactions)
            {
                yield return new List<object?>
                {
                    sr++,
                    t.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                    t.CategoryLabel,
                    t.Reason,
                    t.CustomerName ?? "",
                    t.PaymentType ?? "",
                    t.FinanceName ?? "",
                    t.VinNumber ?? t.ChassisNumber ?? "",
                    t.RequestedAmount,
                    t.ApprovedPaymentAmount,
                    t.IsDebit() ? t.Amount : null,
                    t.IsCredit() ? t.Amount : null,
                    t.BalanceAfterTransaction,
                    t.PaymentSubmittedDate?.ToString("yyyy-MM-dd HH:mm"),
                    t.PaymentApprovedDate?.ToString("yyyy-MM-dd HH:mm"),
                    t.PaymentReceivedDate?.ToString("yyyy-MM-dd"),
                    t.Remarks ?? ""
                };
            }
        }

        public static IActionResult ToFileResult(
            Controller controller,
            int accountId,
            string subdealerName,
            IEnumerable<AccountTransactionDto> transactions)
        {
            var safeName = string.Join("_", subdealerName.Split(Path.GetInvalidFileNameChars()));
            return ExcelExportHelper.ToFileResult(
                controller,
                $"statement_{safeName}_{accountId}_{DateTime.Now:yyyyMMdd}.xlsx",
                Headers,
                BuildRows(transactions),
                "Statement");
        }
    }
}

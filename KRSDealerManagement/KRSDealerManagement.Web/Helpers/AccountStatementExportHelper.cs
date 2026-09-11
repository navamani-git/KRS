using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Helpers
{
    public static class AccountStatementExportHelper
    {
        public static readonly string[] Headers =
        {
            "#", "Credit/Debit Date", "Type", "Description", "Customer", "Pay Type", "Finance", "VIN",
            "Requested Amt", "Approved Amt", "Debit", "Credit",
            "Submitted Date", "Approved Date", "Received Date", "Remarks"
        };

        public static IEnumerable<IReadOnlyList<object?>> BuildRows(IEnumerable<AccountTransactionDto> transactions)
        {
            var sr = 1;
            var list = transactions.Where(t => t.TransactionId > 0).ToList();
            foreach (var t in list)
            {
                yield return new List<object?>
                {
                    sr++,
                    t.StatementDate.ToString("yyyy-MM-dd HH:mm"),
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
                    t.PaymentSubmittedDate?.ToString("yyyy-MM-dd HH:mm"),
                    t.PaymentApprovedDate?.ToString("yyyy-MM-dd HH:mm"),
                    t.PaymentReceivedDate?.ToString("yyyy-MM-dd"),
                    t.Remarks ?? ""
                };
            }

            var totalApproved = list.Where(t => t.ApprovedPaymentAmount.HasValue).Sum(t => t.ApprovedPaymentAmount!.Value);
            var totalDebit = list.Where(t => t.IsDebit()).Sum(t => t.Amount);
            var totalCredit = list.Where(t => t.IsCredit()).Sum(t => t.Amount);
            var net = totalCredit - totalDebit;

            yield return new List<object?>
            {
                "",
                "",
                "TOTAL",
                $"({list.Count} transactions)",
                "", "", "", "",
                null,
                totalApproved,
                totalDebit,
                totalCredit,
                "", "", "",
                $"Net (Credit - Debit): {net:N2}"
            };
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

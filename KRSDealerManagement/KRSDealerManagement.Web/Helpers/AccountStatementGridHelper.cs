using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class AccountStatementGridHelper
    {
        public static async Task<IReadOnlyList<AccountTransactionDto>> LoadPageAsync(
            IMediator mediator,
            Controller controller,
            int accountId,
            DateTime? fromDate,
            DateTime? toDate,
            int? page,
            int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(controller, GridIds.AccountStatement);

            var transactions = (await mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = accountId,
                FromDate = fromDate,
                ToDate = toDate
            })).ToList();

            transactions = GridScreenFilterHelper.ApplyAccountStatement(transactions, columnFilters).ToList();
            ApplyTotals(controller.ViewBag, transactions);

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(transactions, page, pageSize);
            ListPagingHelper.ApplyToViewBag(controller.ViewBag, pageInfo);
            controller.ViewBag.GridId = GridIds.AccountStatement;
            controller.ViewBag.AccountId = accountId;

            return pageItems.ToList();
        }

        public static void ApplyTotals(dynamic viewBag, IReadOnlyList<AccountTransactionDto> transactions)
        {
            viewBag.StatementTotals = new AccountStatementTotalsViewModel
            {
                TotalApproved = transactions.Where(t => t.ApprovedPaymentAmount.HasValue).Sum(t => t.ApprovedPaymentAmount!.Value),
                TotalDebit = transactions.Where(t => t.IsDebit()).Sum(t => t.Amount),
                TotalCredit = transactions.Where(t => t.IsCredit()).Sum(t => t.Amount),
                TransactionCount = transactions.Count
            };
        }
    }
}

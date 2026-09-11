using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
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
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var columnFilters = GridViewHelper.SetupGridFilters(controller, GridIds.AccountStatement);

            var transactions = (await mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = accountId,
                FromDate = from,
                ToDate = to
            })).ToList();

            var allTransactions = (await mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = accountId
            })).ToList();

            var balance = await mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = accountId });

            transactions = GridScreenFilterHelper.ApplyAccountStatement(transactions, columnFilters).ToList();
            transactions = AccountStatementOpeningHelper.AppendLedgerOpeningRows(
                transactions,
                balance,
                balance?.CreatedDate,
                from,
                allTransactions).ToList();
            ApplyTotals(controller.ViewBag, transactions.Where(t => t.TransactionId > 0).ToList());

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(transactions, page, pageSize);
            ListPagingHelper.ApplyToViewBag(controller.ViewBag, pageInfo);
            controller.ViewBag.GridId = GridIds.AccountStatement;
            controller.ViewBag.AccountId = accountId;
            controller.ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            controller.ViewBag.ToDate = to.ToString("yyyy-MM-dd");

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

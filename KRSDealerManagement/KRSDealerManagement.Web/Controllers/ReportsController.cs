using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 2, 3, 4)]
    public class ReportsController : Controller
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public IActionResult Index()
        {
            var session = HttpContext.Session;
            if (!SessionHelper.IsAuthenticated(session))
                return RedirectToAction("Login", "Account");

            // Finance/System: admin_reports · Subdealer: reports
            if (!SessionHelper.HasMenuAccess(session, StaffMenuAccess.Reports)
                && !SessionHelper.HasMenuAccess(session, MenuKeys.Reports))
                return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        // GET: Reports/AccountStatement  (Subdealer's own statement)
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.AccountStatements)]
        public async Task<IActionResult> AccountStatement(DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
            {
                return View(Enumerable.Empty<KRSDealerManagement.Application.DTOs.AccountTransactionDto>());
            }

            var transactions = await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = account.AccountId,
                FromDate = fromDate,
                ToDate = toDate
            });

            var balance = await _mediator.Send(new GetAccountBalanceQuery
            {
                SubdealerAccountId = account.AccountId
            });

            ViewBag.Balance = balance;

            return View(transactions);
        }
    }
}

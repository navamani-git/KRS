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
        public async Task<IActionResult> AccountStatement(DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
            {
                ViewBag.AccountId = null;
                return View(Enumerable.Empty<KRSDealerManagement.Application.DTOs.AccountTransactionDto>());
            }

            var pageItems = await AccountStatementGridHelper.LoadPageAsync(
                _mediator, this, account.AccountId, fromDate, toDate, page, pageSize);

            var balance = await _mediator.Send(new GetAccountBalanceQuery
            {
                SubdealerAccountId = account.AccountId
            });

            ViewBag.Balance = balance;
            ViewBag.AccountId = account.AccountId;

            return View(pageItems);
        }
    }
}

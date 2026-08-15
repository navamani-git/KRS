using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Services;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 3)] // System admin + finance only (not branch manager)
    public class AccountsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IQueryStringCrypto _queryCrypto;

        public AccountsController(IMediator mediator, IQueryStringCrypto queryCrypto)
        {
            _mediator = mediator;
            _queryCrypto = queryCrypto;
        }

        // GET: Accounts - list all subdealer accounts
        public async Task<IActionResult> Index(int? subdealerId, int? page)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.Subdealers = subdealers;
            ViewBag.SelectedSubdealerId = subdealerId;

            IEnumerable<KRSDealerManagement.Application.DTOs.SubdealerAccountDto> accounts;
            if (subdealerId.HasValue)
            {
                if (scope.HasValue && subdealers.All(s => s.UserId != subdealerId.Value))
                {
                    TempData["Error"] = "Subdealer is outside your dealership.";
                    return RedirectToAction(nameof(Index));
                }
                accounts = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = subdealerId.Value });
            }
            else
            {
                var allAccounts = new List<KRSDealerManagement.Application.DTOs.SubdealerAccountDto>();
                foreach (var s in subdealers)
                {
                    var accs = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = s.UserId });
                    allAccounts.AddRange(accs);
                }
                accounts = allAccounts;
            }

            var accountList = accounts.ToList();
            ViewBag.GrandTotalAccounts = accountList.Count;
            ViewBag.GrandCurrent = accountList.Sum(a => a.CurrentBalance);
            ViewBag.GrandReserved = accountList.Sum(a => a.ReservedAmount);
            ViewBag.GrandAvailable = accountList.Sum(a => a.AvailableBalance);

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(accountList, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public async Task<IActionResult> Export(int? subdealerId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });

            IEnumerable<KRSDealerManagement.Application.DTOs.SubdealerAccountDto> accounts;
            if (subdealerId.HasValue)
            {
                if (scope.HasValue && subdealers.All(s => s.UserId != subdealerId.Value))
                    return RedirectToAction(nameof(Index));
                accounts = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = subdealerId.Value });
            }
            else
            {
                var allAccounts = new List<KRSDealerManagement.Application.DTOs.SubdealerAccountDto>();
                foreach (var s in subdealers)
                {
                    var accs = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = s.UserId });
                    allAccounts.AddRange(accs);
                }
                accounts = allAccounts;
            }

            var accountList = accounts.ToList();
            var headers = new[] { "Subdealer", "Account", "Type", "Current Balance", "Reserved", "Available", "Status" };
            var rows = accountList.Select(a => (IReadOnlyList<object?>)new List<object?>
            {
                a.SubdealerName, a.AccountName, a.AccountType, a.CurrentBalance, a.ReservedAmount,
                a.AvailableBalance, a.IsActive ? "Active" : "Inactive"
            });
            return ExcelExportHelper.ToFileResult(this, $"accounts_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Accounts");
        }

        // GET: Accounts/Create — multi-account creation disabled (one balance per subdealer)
        public IActionResult Create(int? subdealerId)
        {
            TempData["Info"] = "Each subdealer has a single balance created automatically. Extra accounts are not used.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { subdealerId }));
        }

        // POST: Accounts/Create — blocked
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int subdealerId, string accountName, string accountType, string description, decimal initialBalance)
        {
            TempData["Error"] = "Creating extra accounts is disabled. Each subdealer uses one balance.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { subdealerId }));
        }

        // GET: Accounts/Statement/5 - View account statement
        public async Task<IActionResult> Statement(int id)
        {
            // Load transactions for this account
            var transactions = await _mediator.Send(new GetAccountTransactionsQuery { AccountId = id });
            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = id });

            ViewBag.Balance = balance;
            ViewBag.AccountId = id;

            return View(transactions);
        }
    }
}

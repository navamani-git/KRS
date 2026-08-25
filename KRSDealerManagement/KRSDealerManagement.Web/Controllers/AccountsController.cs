using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Services;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    public class AccountsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IQueryStringCrypto _queryCrypto;

        public AccountsController(IMediator mediator, IQueryStringCrypto queryCrypto)
        {
            _mediator = mediator;
            _queryCrypto = queryCrypto;
        }

        // GET: Accounts — finance/admin/branch manager balances (scoped by dealership)
        [AuthorizeRole(1, 3, 4)]
        [AuthorizeMenu(StaffMenuAccess.Balances)]
        public async Task<IActionResult> Index(int? subdealerId, int? page, int? pageSize)
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

            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Accounts);
            accountList = GridScreenFilterHelper.ApplyAccounts(accountList, columnFilters).ToList();

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(accountList, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        [AuthorizeRole(1, 3, 4)]
        [AuthorizeMenu(StaffMenuAccess.Balances)]
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

        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Balances)]
        public IActionResult Create(int? subdealerId)
        {
            TempData["Info"] = "Each subdealer has a single balance created automatically. Extra accounts are not used.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { subdealerId }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Balances)]
        public IActionResult Create(int subdealerId, string accountName, string accountType, string description, decimal initialBalance)
        {
            TempData["Error"] = "Creating extra accounts is disabled. Each subdealer uses one balance.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { subdealerId }));
        }

        /// <summary>
        /// Account statement — read-only for branch managers (their subdealers only).
        /// Finance admin / system admin can open from Balances or subdealer details.
        /// </summary>
        [AuthorizeRole(1, 3, 4)]
        public async Task<IActionResult> Statement(int id, DateTime? fromDate, DateTime? toDate)
        {
            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = id });
            if (balance == null)
            {
                TempData["Error"] = "Account not found.";
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!await IsSubdealerInScopeAsync(balance.SubdealerId))
            {
                TempData["Error"] = "This account is outside your dealership scope.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var isBranchManager = SessionHelper.IsBranchManager(HttpContext.Session);
            var isFinanceOrAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);

            if (isBranchManager && !isFinanceOrAdmin)
            {
                ViewBag.IsReadOnlyStatement = true;
                ViewBag.StatementBackUrl = QueryStringUrlHelper.EncryptedAction(
                    Url, _queryCrypto, "Details", new { id = balance.SubdealerId }, "Subdealers");
            }
            else if (!isFinanceOrAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            else
            {
                ViewBag.StatementBackUrl = Url.Action(nameof(Index));
            }

            var transactions = (await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = id,
                FromDate = fromDate,
                ToDate = toDate
            })).ToList();
            ViewBag.Balance = balance;
            ViewBag.AccountId = id;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(transactions);
        }

        [AuthorizeRole(1, 3, 4)]
        public async Task<IActionResult> ExportStatement(int id, DateTime? fromDate, DateTime? toDate)
        {
            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = id });
            if (balance == null)
                return RedirectToAction("AccessDenied", "Account");

            if (!await IsSubdealerInScopeAsync(balance.SubdealerId))
                return RedirectToAction("AccessDenied", "Account");

            var isBranchManager = SessionHelper.IsBranchManager(HttpContext.Session);
            var isFinanceOrAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);
            if (!isFinanceOrAdmin && !isBranchManager)
                return RedirectToAction("AccessDenied", "Account");

            var transactions = await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = id,
                FromDate = fromDate,
                ToDate = toDate
            });

            return AccountStatementExportHelper.ToFileResult(this, id, balance.SubdealerName, transactions);
        }

        private async Task<bool> IsSubdealerInScopeAsync(int subdealerUserId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var detail = await _mediator.Send(new GetSubdealerDetailQuery
            {
                UserId = subdealerUserId,
                DealershipId = scope
            });
            return detail != null;
        }

        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountAdjustments)]
        public async Task<IActionResult> Adjust(int? subdealerId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.Subdealers = subdealers;
            ViewBag.SelectedSubdealerId = subdealerId;

            if (subdealerId.HasValue)
            {
                var accounts = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = subdealerId.Value });
                ViewBag.Account = accounts.FirstOrDefault();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountAdjustments)]
        public async Task<IActionResult> Adjust(
            int subdealerId, string adjustmentType, decimal amount, string description, string? remarks)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            if (!await IsSubdealerInScopeAsync(subdealerId))
            {
                TempData["Error"] = "Subdealer is outside your dealership scope.";
                return RedirectToAction(nameof(Adjust));
            }

            try
            {
                await _mediator.Send(new AdjustSubdealerAccountCommand
                {
                    SubdealerId = subdealerId,
                    AdjustmentType = adjustmentType,
                    Amount = amount,
                    Description = description.Trim(),
                    Remarks = remarks?.Trim(),
                    AdjustedBy = adminId.Value
                });

                TempData["Success"] = $"{adjustmentType} of ₹{amount:N2} applied successfully.";
                return RedirectToAction(nameof(Adjust), new { subdealerId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Adjust), new { subdealerId });
            }
        }
    }
}

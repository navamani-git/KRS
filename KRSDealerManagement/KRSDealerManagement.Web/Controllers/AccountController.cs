using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (SessionHelper.IsAuthenticated(HttpContext.Session))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please enter both username and password.";
                return View();
            }

            var command = new LoginCommand
            {
                Username = username,
                Password = password,
                RememberMe = rememberMe
            };

            var result = await _mediator.Send(command);

            if (!result.Succeeded || result.Data == null)
            {
                TempData["Error"] = result.Errors.FirstOrDefault() ?? "Login failed. Please try again.";
                return View();
            }

            // Set user session (hierarchy from UserOrgRoles + RoleMenus)
            SessionHelper.SetUserSession(
                HttpContext.Session,
                result.Data.UserId,
                result.Data.Username,
                result.Data.FullName,
                result.Data.UserRole,
                result.Data.RoleName,
                result.Data.RoleCode,
                result.Data.DealershipId,
                result.Data.DealershipName,
                result.Data.SubDealerId,
                result.Data.AccessibleMenuKeys
            );

            TempData["Success"] = $"Welcome back, {result.Data.FullName}!";

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            var fullName = SessionHelper.GetFullName(HttpContext.Session);
            SessionHelper.ClearSession(HttpContext.Session);
            TempData["Success"] = $"Goodbye, {fullName}! You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Subdealer account statement — /Account/Statement
        /// </summary>
        [HttpGet]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.AccountStatements)]
        public async Task<IActionResult> Statement(DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction(nameof(Login));

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
            {
                return View("~/Views/Reports/AccountStatement.cshtml",
                    Enumerable.Empty<KRSDealerManagement.Application.DTOs.AccountTransactionDto>());
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
            return View("~/Views/Reports/AccountStatement.cshtml", transactions);
        }

        [HttpGet]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.AccountStatements)]
        public async Task<IActionResult> ExportStatement(DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction(nameof(Login));

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
                return RedirectToAction(nameof(Statement));

            var balance = await _mediator.Send(new GetAccountBalanceQuery
            {
                SubdealerAccountId = account.AccountId
            });

            var transactions = await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = account.AccountId,
                FromDate = fromDate,
                ToDate = toDate
            });

            return AccountStatementExportHelper.ToFileResult(
                this, account.AccountId, balance?.SubdealerName ?? account.AccountName, transactions);
        }
    }
}

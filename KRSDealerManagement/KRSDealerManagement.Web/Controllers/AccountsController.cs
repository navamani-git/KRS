using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Services;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    public class AccountsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IQueryStringCrypto _queryCrypto;
        private readonly IUnitOfWork _unitOfWork;

        public AccountsController(IMediator mediator, IQueryStringCrypto queryCrypto, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _queryCrypto = queryCrypto;
            _unitOfWork = unitOfWork;
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
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Accounts);
            accountList = GridScreenFilterHelper.ApplyAccounts(accountList, columnFilters).ToList();
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
        public async Task<IActionResult> Statement(int id, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
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

            var pageItems = await AccountStatementGridHelper.LoadPageAsync(
                _mediator, this, id, fromDate, toDate, page, pageSize);

            ViewBag.Balance = balance;
            ViewBag.AccountId = id;

            return View(pageItems);
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

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var transactions = await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = id,
                FromDate = from,
                ToDate = to
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

        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountTransactions)]
        public async Task<IActionResult> Transactions(int? accountId, DateTime? fromDate, DateTime? toDate)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.Subdealers = subdealers;
            var accountOptions = new List<KRSDealerManagement.Application.DTOs.SubdealerAccountDto>();
            foreach (var s in subdealers)
            {
                var accs = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = s.UserId });
                accountOptions.AddRange(accs);
            }
            ViewBag.AccountOptions = accountOptions.OrderBy(a => a.SubdealerName).ToList();
            ViewBag.SelectedAccountId = accountId;
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");

            if (!accountId.HasValue)
                return View(Enumerable.Empty<KRSDealerManagement.Application.DTOs.AccountTransactionDto>());

            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = accountId.Value });
            if (balance == null || !await IsSubdealerInScopeAsync(balance.SubdealerId))
            {
                TempData["Error"] = "Account not found or outside your scope.";
                return View(Enumerable.Empty<KRSDealerManagement.Application.DTOs.AccountTransactionDto>());
            }

            ViewBag.Balance = balance;
            var transactions = await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = accountId.Value,
                FromDate = from,
                ToDate = to
            });

            return View(transactions);
        }

        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountTransactions)]
        public async Task<IActionResult> TransactionCorrections(int? accountId, DateTime? fromDate, DateTime? toDate)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.Subdealers = subdealers;
            ViewBag.SelectedAccountId = accountId;
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");

            var corrections = await _mediator.Send(new GetAccountTransactionCorrectionsQuery
            {
                AccountId = accountId,
                FromDate = from,
                ToDate = to
            });

            return View(corrections);
        }

        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountTransactions)]
        public async Task<IActionResult> AdminEditTransaction(int id)
        {
            var transaction = await _unitOfWork.AccountTransactions.GetByIdAsync(id);
            if (transaction == null || transaction.IsDeleted)
            {
                TempData["Error"] = "Transaction not found.";
                return RedirectToAction(nameof(Transactions));
            }

            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = transaction.AccountId });
            if (balance == null || !await IsSubdealerInScopeAsync(balance.SubdealerId))
            {
                TempData["Error"] = "Transaction is outside your scope.";
                return RedirectToAction(nameof(Transactions));
            }

            var enriched = (await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = transaction.AccountId,
                IncludeDeleted = true
            })).FirstOrDefault(t => t.TransactionId == id);

            if (enriched == null)
            {
                TempData["Error"] = "Transaction not found.";
                return RedirectToAction(nameof(Transactions));
            }

            ViewBag.Balance = balance;
            ViewBag.TransactionTypes = BuildTransactionTypeOptions();
            ViewBag.PaymentTypes = (await _unitOfWork.PaymentTypes.GetAllAsync())
                .Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList();
            ViewBag.FinanceNames = (await _unitOfWork.FinanceNames.GetAllAsync())
                .Where(f => f.IsActive).OrderBy(f => f.FinanceName).ToList();

            return View(enriched);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountTransactions)]
        public async Task<IActionResult> AdminEditTransaction(
            int transactionId,
            int transactionType,
            decimal amount,
            DateTime transactionDate,
            string reason,
            string? remarks,
            decimal? requestedAmount,
            decimal? approvedPaymentAmount,
            DateTime? paymentSubmittedDate,
            DateTime? paymentApprovedDate,
            DateTime? paymentReceivedDate,
            string? customerName,
            int? paymentTypeId,
            int? financeNameId,
            string? vinNumber,
            decimal? commissionAmount,
            string correctionReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(correctionReason) || correctionReason.Trim().Length < 5)
            {
                TempData["Error"] = "Correction reason is required (min 5 characters).";
                return this.RedirectEncrypted(nameof(AdminEditTransaction), new { id = transactionId });
            }

            var transaction = await _unitOfWork.AccountTransactions.GetByIdAsync(transactionId);
            if (transaction == null || transaction.IsDeleted)
            {
                TempData["Error"] = "Transaction not found.";
                return RedirectToAction(nameof(Transactions));
            }

            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = transaction.AccountId });
            if (balance == null || !await IsSubdealerInScopeAsync(balance.SubdealerId))
            {
                TempData["Error"] = "Transaction is outside your scope.";
                return RedirectToAction(nameof(Transactions));
            }

            try
            {
                var ok = await _mediator.Send(new AdminEditAccountTransactionCommand
                {
                    TransactionId = transactionId,
                    TransactionType = transactionType,
                    Amount = amount,
                    TransactionDate = transactionDate,
                    Reason = reason,
                    Remarks = remarks,
                    RequestedAmount = requestedAmount,
                    ApprovedPaymentAmount = approvedPaymentAmount,
                    PaymentSubmittedDate = paymentSubmittedDate,
                    PaymentApprovedDate = paymentApprovedDate,
                    PaymentReceivedDate = paymentReceivedDate,
                    CustomerName = customerName,
                    PaymentTypeId = paymentTypeId,
                    FinanceNameId = financeNameId,
                    VinNumber = vinNumber,
                    CommissionAmount = commissionAmount,
                    CorrectionReason = correctionReason.Trim(),
                    CorrectedBy = userId.Value,
                    CorrectedByName = SessionHelper.GetFullName(HttpContext.Session)
                        ?? SessionHelper.GetUsername(HttpContext.Session) ?? "Admin"
                });

                if (!ok)
                {
                    TempData["Error"] = "Unable to update transaction.";
                    return this.RedirectEncrypted(nameof(AdminEditTransaction), new { id = transactionId });
                }

                TempData["Success"] = "Transaction updated. Subdealer ledger reflects corrected values only.";
                return RedirectToAction(nameof(Transactions), new { accountId = transaction.AccountId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return this.RedirectEncrypted(nameof(AdminEditTransaction), new { id = transactionId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        [AuthorizeMenu(StaffMenuAccess.AccountTransactions)]
        public async Task<IActionResult> AdminDeleteTransaction(int transactionId, string deleteReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(deleteReason) || deleteReason.Trim().Length < 5)
            {
                TempData["Error"] = "Delete reason is required (min 5 characters).";
                return RedirectToAction(nameof(Transactions));
            }

            var transaction = await _unitOfWork.AccountTransactions.GetByIdAsync(transactionId);
            if (transaction == null || transaction.IsDeleted)
            {
                TempData["Error"] = "Transaction not found.";
                return RedirectToAction(nameof(Transactions));
            }

            var balance = await _mediator.Send(new GetAccountBalanceQuery { SubdealerAccountId = transaction.AccountId });
            if (balance == null || !await IsSubdealerInScopeAsync(balance.SubdealerId))
            {
                TempData["Error"] = "Transaction is outside your scope.";
                return RedirectToAction(nameof(Transactions));
            }

            try
            {
                var ok = await _mediator.Send(new AdminDeleteAccountTransactionCommand
                {
                    TransactionId = transactionId,
                    DeleteReason = deleteReason.Trim(),
                    DeletedBy = userId.Value,
                    DeletedByName = SessionHelper.GetFullName(HttpContext.Session)
                        ?? SessionHelper.GetUsername(HttpContext.Session) ?? "Admin"
                });

                TempData[ok ? "Success" : "Error"] = ok
                    ? "Transaction removed from subdealer ledger. Correction recorded for admin audit."
                    : "Unable to delete transaction.";
                return RedirectToAction(nameof(Transactions), new { accountId = transaction.AccountId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Transactions), new { accountId = transaction.AccountId });
            }
        }

        private static IEnumerable<(int Value, string Label)> BuildTransactionTypeOptions()
        {
            yield return (1, "Debit");
            yield return (2, "Credit");
            yield return (5, "Debit (Alt)");
            yield return (6, "Credit (Alt)");
            yield return (7, "Commission Credit");
            yield return (8, "Commission Rejected");
            yield return (3, "Reserved");
            yield return (4, "Released");
        }
    }
}

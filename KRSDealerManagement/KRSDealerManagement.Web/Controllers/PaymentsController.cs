using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Web.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;
        private readonly IStatusLookupService _statuses;

        public PaymentsController(IMediator mediator, IUnitOfWork unitOfWork, IWebHostEnvironment env, IStatusLookupService statuses)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _env = env;
            _statuses = statuses;
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyPayments)]
        public async Task<IActionResult> MyPayments(int? status, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.MyPayments);
            var payments = await _mediator.Send(new GetPaymentsQuery
            {
                SubdealerId = userId.Value,
                Status = status,
                FromDate = from,
                ToDate = to,
                ColumnFilters = columnFilters
            });

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(payments, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.PaymentTypes = (await _unitOfWork.PaymentTypes.GetAllAsync())
                .Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList();
            ViewBag.FinanceNames = (await _unitOfWork.FinanceNames.GetAllAsync())
                .Where(f => f.IsActive).OrderBy(f => f.FinanceName).ToList();
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Payment);

            return View(pageItems);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyPayments)]
        public async Task<IActionResult> ExportMyPayments(int? status, DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var payments = (await _mediator.Send(new GetPaymentsQuery
            {
                SubdealerId = userId.Value,
                Status = status,
                FromDate = from,
                ToDate = to
            })).ToList();

            var headers = new[] { "ID", "Amount", "Type", "Payment Date", "Status", "Customer", "Finance", "VIN", "Remarks", "Submitted", "Approved", "Received Date", "Received Amt" };
            var rows = payments.Select(p => (IReadOnlyList<object?>)new List<object?>
            {
                p.PaymentId, p.Amount, p.GetPaymentTypeDisplay(), p.PaymentDate, p.GetStatusDisplay(),
                p.CustomerName ?? "", p.FinanceName ?? "", p.VinNumber ?? "", p.SubdealerRemarks ?? "",
                p.CreatedDate, p.ProcessedDate, p.ActualReceivedDate, p.ActualReceivedAmount ?? p.Amount
            });
            return ExcelExportHelper.ToFileResult(this, $"my_payments_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "My Payments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyPayments)]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Submit(
            decimal amount,
            int paymentTypeId,
            DateTime paymentDate,
            string? subdealerRemarks,
            string? otherPaymentType,
            string? customerName,
            int? financeNameId,
            string? vinNumber,
            IFormFile? paymentProof,
            IFormFile? paymentProof2)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (amount <= 0)
            {
                TempData["Error"] = "Payment amount must be greater than zero.";
                return RedirectToAction(nameof(MyPayments));
            }

            if (paymentProof == null || paymentProof.Length == 0)
            {
                TempData["Error"] = "Payment proof is required.";
                return RedirectToAction(nameof(MyPayments));
            }

            var type = (await _unitOfWork.PaymentTypes.GetAllAsync())
                .FirstOrDefault(t => t.PaymentTypeId == paymentTypeId && t.IsActive);
            if (type == null)
            {
                TempData["Error"] = "Invalid payment type.";
                return RedirectToAction(nameof(MyPayments));
            }

            var requiresFinance = type.RequiresFinanceDetails
                || type.TypeCode.Equals("FINANCE", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(customerName))
            {
                TempData["Error"] = "Customer name is required for all payments.";
                return RedirectToAction(nameof(MyPayments));
            }

            if (requiresFinance)
            {
                if (!financeNameId.HasValue || financeNameId.Value <= 0)
                {
                    TempData["Error"] = "Finance name is required for Finance payments.";
                    return RedirectToAction(nameof(MyPayments));
                }
                if (string.IsNullOrWhiteSpace(vinNumber))
                {
                    TempData["Error"] = "VIN / Chassis number is required for Finance payments.";
                    return RedirectToAction(nameof(MyPayments));
                }
            }

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
            {
                TempData["Error"] = "No account found for your profile. Please contact administrator.";
                return RedirectToAction(nameof(MyPayments));
            }

            try
            {
                // Prefer content root Files/… (as requested); fall back to web root
                var filesRoot = Path.Combine(_env.ContentRootPath, "Files");
                Directory.CreateDirectory(filesRoot);

                var proof1 = await PaymentFileHelper.SaveAsync(paymentProof, _env.ContentRootPath);
                string? proof2 = null;
                if (paymentProof2 != null && paymentProof2.Length > 0)
                    proof2 = await PaymentFileHelper.SaveAsync(paymentProof2, _env.ContentRootPath);

                var typeLabel = type.TypeName;
                if (type.TypeCode.Equals("OTHERS", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(otherPaymentType))
                {
                    typeLabel = $"Others ({otherPaymentType.Trim()})";
                }

                var remarks = subdealerRemarks?.Trim();
                if (type.TypeCode.Equals("OTHERS", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(otherPaymentType))
                {
                    remarks = $"Other: {otherPaymentType.Trim()}. {remarks}".Trim();
                }

                var paymentId = await _mediator.Send(new CreatePaymentCommand
                {
                    AccountId = account.AccountId,
                    SubdealerId = userId.Value,
                    Amount = amount,
                    PaymentTypeId = type.PaymentTypeId,
                    PaymentType = typeLabel,
                    PaymentDate = paymentDate,
                    SubdealerRemarks = remarks,
                    OtherPaymentType = otherPaymentType,
                    CustomerName = customerName.Trim().ToUpperInvariant(),
                    FinanceNameId = requiresFinance ? financeNameId : null,
                    VinNumber = vinNumber,
                    PaymentProofPath = proof1,
                    PaymentProof2Path = proof2,
                    RequiresFinanceDetails = requiresFinance,
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Payment of ₹{amount:N2} submitted (#{paymentId}). Awaiting approval.";
                return RedirectToAction(nameof(MyPayments));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(MyPayments));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyPayments)]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> SubmitCreditRequest(
            decimal? amount,
            string? modelName,
            string? colorName,
            string? chassisNumber,
            string? reason,
            IFormFile? paymentProof)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (!amount.HasValue || amount.Value <= 0)
            {
                TempData["Error"] = "Credit request amount must be greater than zero.";
                return RedirectToAction(nameof(MyPayments));
            }

            var creditType = (await _unitOfWork.PaymentTypes.GetAllAsync())
                .FirstOrDefault(t => t.IsActive
                    && t.TypeCode.Equals(CreditRequestHelper.TypeCode, StringComparison.OrdinalIgnoreCase));
            if (creditType == null)
            {
                TempData["Error"] = "Credit Request payment type is not configured. Contact administrator.";
                return RedirectToAction(nameof(MyPayments));
            }

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
            {
                TempData["Error"] = "No account found for your profile. Please contact administrator.";
                return RedirectToAction(nameof(MyPayments));
            }

            try
            {
                string? proofPath = null;
                if (paymentProof != null && paymentProof.Length > 0)
                    proofPath = await PaymentFileHelper.SaveAsync(paymentProof, _env.ContentRootPath);

                var paymentId = await _mediator.Send(new CreatePaymentCommand
                {
                    AccountId = account.AccountId,
                    SubdealerId = userId.Value,
                    Amount = amount.Value,
                    PaymentTypeId = creditType.PaymentTypeId,
                    PaymentType = creditType.TypeName,
                    PaymentDate = DateTime.Today,
                    SubdealerRemarks = reason?.Trim(),
                    VinNumber = chassisNumber,
                    CreditRequestModelName = modelName,
                    CreditRequestColorName = colorName,
                    PaymentProofPath = proofPath,
                    IsCreditRequest = true,
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Credit request of ₹{amount.Value:N2} submitted (#{paymentId}). Awaiting approval.";
                return RedirectToAction(nameof(MyPayments));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(MyPayments));
            }
        }

        [AuthorizeRole(1, 2, 3, 4)]
        public IActionResult ViewProof(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
                return BadRequest();

            var absolute = Path.Combine(_env.ContentRootPath, path.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(absolute))
                return NotFound();

            var contentType = PaymentFileHelper.GetContentType(absolute);
            return PhysicalFile(absolute, contentType);
        }

        [AuthorizeRole(1, 2, 3, 4)]
        public IActionResult DownloadProof(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
                return BadRequest();

            var absolute = Path.Combine(_env.ContentRootPath, path.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(absolute))
                return NotFound();

            var contentType = PaymentFileHelper.GetContentType(absolute);
            return PhysicalFile(absolute, contentType, Path.GetFileName(absolute));
        }

        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Payments)]
        public async Task<IActionResult> Index(int? status, int? subdealerId, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Payments);
            var payments = await _mediator.Send(new GetPaymentsQuery
            {
                Status = status,
                SubdealerId = subdealerId,
                FromDate = from,
                ToDate = to,
                ColumnFilters = columnFilters
            });

            var subdealers = (await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope })).ToList();
            if (scope.HasValue)
            {
                var allowed = subdealers.Select(s => s.UserId).ToHashSet();
                payments = payments.Where(p => allowed.Contains(p.SubdealerId));
            }

            var paymentList = payments.ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(paymentList, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.Subdealers = subdealers;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSubdealerId = subdealerId;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.PendingCount = paymentList.Count(p => p.Status == 0);
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Payment);

            return View(pageItems);
        }

        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Payments)]
        public async Task<IActionResult> Export(int? status, int? subdealerId, DateTime? fromDate, DateTime? toDate)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var payments = await _mediator.Send(new GetPaymentsQuery
            {
                Status = status,
                SubdealerId = subdealerId,
                FromDate = from,
                ToDate = to
            });

            var subdealers = (await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope })).ToList();
            if (scope.HasValue)
            {
                var allowed = subdealers.Select(s => s.UserId).ToHashSet();
                payments = payments.Where(p => allowed.Contains(p.SubdealerId));
            }

            var paymentList = payments.ToList();
            var headers = new[] { "ID", "Subdealer", "Requested Amt", "Received Amt", "Type", "Payment Date", "Status", "Applied", "Customer", "Finance", "VIN", "Submitted", "Approved", "Received Date" };
            var rows = paymentList.Select(p => (IReadOnlyList<object?>)new List<object?>
            {
                p.PaymentId, p.SubdealerName, p.Amount, p.ActualReceivedAmount ?? (p.Status == 1 ? p.Amount : null),
                p.GetPaymentTypeDisplay(), p.PaymentDate, p.GetStatusDisplay(),
                p.IsApplied ? "Yes" : "No", p.CustomerName ?? "", p.FinanceName ?? "", p.VinNumber ?? "",
                p.CreatedDate, p.ProcessedDate, p.ActualReceivedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"payments_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Payments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Payments)]
        public async Task<IActionResult> Approve(
            int id, string remarks, bool applyToBalance,
            decimal? actualReceivedAmount, DateTime actualReceivedDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                var result = await _mediator.Send(new ApprovePaymentCommand
                {
                    PaymentId = id,
                    ApprovedBy = userId.Value,
                    Remarks = string.IsNullOrWhiteSpace(remarks) ? "Approved" : remarks.Trim(),
                    ApplyToBalance = applyToBalance,
                    ActualReceivedAmount = actualReceivedAmount,
                    ActualReceivedDate = actualReceivedDate
                });

                TempData[result ? "Success" : "Error"] = result
                    ? $"Payment #{id} approved and credited (received amount applied)."
                    : "Payment not found or cannot be approved.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Payments)]
        public async Task<IActionResult> Reject(int id, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Remarks are required when rejecting a payment.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _mediator.Send(new RejectPaymentCommand
                {
                    PaymentId = id,
                    RejectedBy = userId.Value,
                    Remarks = remarks.Trim()
                });

                TempData[result ? "Success" : "Error"] = result
                    ? $"Payment #{id} rejected."
                    : "Payment not found or cannot be rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [AuthorizeRole(1)]
        public async Task<IActionResult> AdminEdit(int id)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(id);
            if (payment == null) { TempData["Error"] = "Payment not found."; return RedirectToAction(nameof(Index)); }

            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true });
            ViewBag.SubdealerName = subdealers.FirstOrDefault(s => s.UserId == payment.SubdealerId)?.GetFullName()
                ?? $"Subdealer #{payment.SubdealerId}";
            ViewBag.PaymentTypes = (await _unitOfWork.PaymentTypes.GetAllAsync())
                .Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToList();
            ViewBag.FinanceNames = (await _unitOfWork.FinanceNames.GetAllAsync())
                .Where(f => f.IsActive).OrderBy(f => f.FinanceName).ToList();
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Payment);

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> AdminEdit(
            int paymentId, decimal amount, decimal? actualReceivedAmount, DateTime? actualReceivedDate,
            int paymentTypeId, DateTime paymentDate, int status,
            string? customerName, int? financeNameId, string? vinNumber, string? subdealerRemarks,
            string correctionReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(correctionReason) || correctionReason.Trim().Length < 5)
            {
                TempData["Error"] = "Correction reason is required (min 5 characters).";
                return this.RedirectEncrypted(nameof(AdminEdit), new { id = paymentId });
            }

            try
            {
                await _mediator.Send(new AdminCorrectPaymentCommand
                {
                    PaymentId = paymentId,
                    Amount = amount,
                    ActualReceivedAmount = actualReceivedAmount,
                    ActualReceivedDate = actualReceivedDate,
                    PaymentTypeId = paymentTypeId,
                    PaymentDate = paymentDate,
                    Status = status,
                    CustomerName = customerName,
                    FinanceNameId = financeNameId,
                    VinNumber = vinNumber,
                    SubdealerRemarks = subdealerRemarks,
                    CorrectionReason = correctionReason.Trim(),
                    CorrectedBy = userId.Value,
                    CorrectedByName = SessionHelper.GetFullName(HttpContext.Session) ?? SessionHelper.GetUsername(HttpContext.Session) ?? "Admin"
                });

                TempData["Success"] = "Payment corrected. Change history recorded for subdealer view.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return this.RedirectEncrypted(nameof(AdminEdit), new { id = paymentId });
            }
        }
    }
}

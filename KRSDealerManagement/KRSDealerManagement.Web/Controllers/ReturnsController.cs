using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    public class ReturnsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IStatusLookupService _statuses;

        public ReturnsController(IMediator mediator, IStatusLookupService statuses)
        {
            _mediator = mediator;
            _statuses = statuses;
        }

        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Index(int? status, int? page, int? pageSize)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Returns);
            var returns = GridScreenFilterHelper.ApplyReturns(
                await _mediator.Send(new GetReturnRequestsQuery { Status = status, DealershipId = scope }),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(returns, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = returns.Count(r => r.Status == UnifiedVehicleStatus.ReturnRequested);
            ViewBag.Statuses = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle))
                .Where(s => UnifiedVehicleStatus.IsReturnPhase(s.StatusValue))
                .ToList();

            return View(pageItems);
        }

        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Export(int? status)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Returns);
            var returns = GridScreenFilterHelper.ApplyReturns(
                await _mediator.Send(new GetReturnRequestsQuery { Status = status, DealershipId = scope }),
                columnFilters).ToList();
            var headers = new[] { "ID", "Order", "Chassis", "Subdealer", "Refund", "Status", "Reason", "Requested", "Processed" };
            var rows = returns.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.ReturnRequestId, r.OrderNumber, r.VehicleChassisNumber, r.SubdealerName ?? r.AccountName, r.RefundAmount,
                r.GetStatusDisplay(), r.ReturnReason, r.CreatedDate, r.ProcessedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"returns_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Returns");
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyReturns)]
        public async Task<IActionResult> MyReturns(int? status, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);

            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.MyReturns);
            var returns = GridScreenFilterHelper.ApplyReturns(
                await _mediator.Send(new GetReturnRequestsQuery
                {
                    SubdealerId = userId.Value,
                    Status = status,
                    FromDate = from,
                    ToDate = to
                }),
                columnFilters,
                myReturns: true).ToList();

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(returns, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.PendingCount = returns.Count(r => r.Status == UnifiedVehicleStatus.ReturnRequested);
            ViewBag.Statuses = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle))
                .Where(s => UnifiedVehicleStatus.IsReturnPhase(s.StatusValue))
                .ToList();

            return View(pageItems);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyReturns)]
        public async Task<IActionResult> ExportMyReturns(int? status, DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);

            var returns = (await _mediator.Send(new GetReturnRequestsQuery
            {
                SubdealerId = userId.Value,
                Status = status,
                FromDate = from,
                ToDate = to
            })).ToList();

            var headers = new[] { "ID", "Order", "Chassis", "Account", "Refund", "Status", "Reason", "Requested", "Processed", "Refund Credited", "Admin Remarks" };
            var rows = returns.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.ReturnRequestId, r.OrderNumber, r.VehicleChassisNumber, r.AccountName, r.RefundAmount,
                r.GetStatusDisplay(), r.ReturnReason, r.CreatedDate, r.ProcessedDate, r.RefundCreditedDate,
                r.AdminRemarks ?? ""
            });
            return ExcelExportHelper.ToFileResult(this, $"my_returns_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "My Returns");
        }

        [HttpGet]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Approve(int id)
        {
            var item = await LoadReturnRequestAsync(id);
            if (item == null || !item.CanBeApproved())
            {
                TempData["Error"] = "Return request not found or cannot be approved.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Approve(
            int id,
            decimal refundAmount,
            string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (id <= 0)
            {
                TempData["Error"] = "Invalid return request.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var item = await LoadReturnRequestAsync(id);
                var result = await _mediator.Send(new ApproveReturnRequestCommand
                {
                    ReturnRequestId = id,
                    ApprovedBy = userId.Value,
                    RefundAmount = refundAmount,
                    Remarks = remarks?.Trim() ?? "",
                    ReassignToSubdealerId = null
                });

                var destination = item?.DealershipLocation ?? "dealer showroom";

                TempData[result ? "Success" : "Error"] = result
                    ? $"Return #{id} approved. ₹{refundAmount:N2} credited to returning subdealer; vehicle returned to {destination}."
                    : "Return request not found or cannot be approved.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Reject(int id)
        {
            var item = await LoadReturnRequestAsync(id);
            if (item == null || !item.CanBeRejected())
            {
                TempData["Error"] = "Return request not found or cannot be rejected.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Reject(int id, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (id <= 0)
            {
                TempData["Error"] = "Invalid return request.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _mediator.Send(new RejectReturnRequestCommand
                {
                    ReturnRequestId = id,
                    RejectedBy = userId.Value,
                    Remarks = remarks?.Trim() ?? ""
                });

                TempData[result ? "Success" : "Error"] = result
                    ? $"Return request #{id} rejected."
                    : "Return request not found or cannot be rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Allocate(int id)
        {
            var item = await LoadReturnRequestAsync(id);
            if (item == null || !item.CanAllocateToSubdealer)
            {
                TempData["Error"] = "Return not found or vehicle is not available in showroom stock for allocation.";
                return RedirectToAction(nameof(Index));
            }

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            ViewBag.Subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.CanViewBalances = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Allocate(int id, int subdealerId, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var item = await LoadReturnRequestAsync(id);
            if (item == null || !item.CanAllocateToSubdealer)
            {
                TempData["Error"] = "Return not found or vehicle is not available for allocation.";
                return RedirectToAction(nameof(Index));
            }

            if (subdealerId <= 0)
            {
                TempData["Error"] = "Please select a subdealer.";
                return this.RedirectEncrypted(nameof(Allocate), new { id });
            }

            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Remarks are required.";
                return this.RedirectEncrypted(nameof(Allocate), new { id });
            }

            try
            {
                var ok = await _mediator.Send(new AllocateShowroomVehicleCommand
                {
                    VehicleId = item.VehicleId,
                    SubdealerId = subdealerId,
                    AllocatedBy = userId.Value,
                    Remarks = remarks.Trim(),
                    ReturnRequestId = id
                });

                TempData[ok ? "Success" : "Error"] = ok
                    ? $"Vehicle {item.VehicleChassisNumber} allocated to subdealer. Amount debited from their wallet."
                    : "Allocation failed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return this.RedirectEncrypted(nameof(Allocate), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<ReturnRequestDto?> LoadReturnRequestAsync(int id)
        {
            if (id <= 0) return null;
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var items = await _mediator.Send(new GetReturnRequestsQuery { ReturnRequestId = id, DealershipId = scope });
            return items.FirstOrDefault();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    public class CommissionsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public CommissionsController(IMediator mediator, IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        // GET: Commissions (Admin - Commission Rates)
        [AuthorizeRole(1)]
        public async Task<IActionResult> Index(int? modelId, bool? activeOnly, int? page)
        {
            var rates = await _mediator.Send(new GetCommissionRatesQuery
            {
                ModelId = modelId,
                ActiveOnly = activeOnly
            });

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(rates, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.Models = models;
            ViewBag.SelectedModelId = modelId;
            ViewBag.ActiveOnly = activeOnly;

            return View(pageItems);
        }

        [AuthorizeRole(1)]
        public async Task<IActionResult> Export(int? modelId, bool? activeOnly)
        {
            var rates = (await _mediator.Send(new GetCommissionRatesQuery
            {
                ModelId = modelId,
                ActiveOnly = activeOnly
            })).ToList();
            var headers = new[] { "Model", "Amount", "Start", "Expiry", "Notes", "Created" };
            var rows = rates.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.ModelName, r.CommissionAmount,
                $"{r.StartYear}-{r.StartMonth:D2}",
                r.ExpiryYear.HasValue && r.ExpiryMonth.HasValue ? $"{r.ExpiryYear}-{r.ExpiryMonth:D2}" : "Ongoing",
                r.Notes ?? "", r.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"commission_rates_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Commission Rates");
        }

        // GET: Commissions/CreateRate (Admin)
        [AuthorizeRole(1)]
        public async Task<IActionResult> CreateRate()
        {
            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            ViewBag.Models = models;
            ViewBag.CurrentMonth = DateTime.Now.Month;
            ViewBag.CurrentYear = DateTime.Now.Year;
            return View();
        }

        // POST: Commissions/CreateRate (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> CreateRate(int modelId, decimal commissionAmount,
            int startMonth, int startYear, int? expiryMonth, int? expiryYear, string notes)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (commissionAmount <= 0)
            {
                TempData["Error"] = "Commission amount must be greater than zero.";
                return RedirectToAction(nameof(CreateRate));
            }

            try
            {
                await _mediator.Send(new CreateCommissionRateCommand
                {
                    ModelId = modelId,
                    CommissionAmount = commissionAmount,
                    StartMonth = startMonth,
                    StartYear = startYear,
                    ExpiryMonth = expiryMonth == 0 ? null : expiryMonth,
                    ExpiryYear = expiryYear == 0 ? null : expiryYear,
                    Notes = notes?.Trim(),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Commission rate ₹{commissionAmount:N2} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(CreateRate));
            }
        }

        [AuthorizeRole(1)]
        public async Task<IActionResult> EditRate(int id)
        {
            var row = await _unitOfWork.CommissionRates.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Commission rate not found.";
                return RedirectToAction(nameof(Index));
            }

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            ViewBag.Models = models;
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> EditRate(int id, decimal commissionAmount, int? expiryMonth, int? expiryYear, string? notes)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var row = await _unitOfWork.CommissionRates.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Commission rate not found.";
                return RedirectToAction(nameof(Index));
            }

            if (commissionAmount <= 0)
            {
                TempData["Error"] = "Commission amount must be greater than zero.";
                return RedirectToAction(nameof(EditRate), new { id });
            }

            row.CommissionAmount = commissionAmount;
            row.ExpiryMonth = expiryMonth == 0 ? null : expiryMonth;
            row.ExpiryYear = expiryYear == 0 ? null : expiryYear;
            row.Notes = notes?.Trim();
            row.ModifiedBy = userId.Value;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.CommissionRates.UpdateAsync(row);
            TempData["Success"] = "Commission rate updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> DeleteRate(int id)
        {
            var row = await _unitOfWork.CommissionRates.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Commission rate not found.";
                return RedirectToAction(nameof(Index));
            }

            row.ExpiryMonth = DateTime.UtcNow.Month;
            row.ExpiryYear = DateTime.UtcNow.Year;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.CommissionRates.UpdateAsync(row);
            TempData["Success"] = "Commission rate ended.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Commissions/Submit (Subdealer - Submit Commission)
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.CommissionSubmit)]
        public async Task<IActionResult> Submit()
        {
            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var colors = await _mediator.Send(new GetVehicleColorsQuery { IsActive = true });
            ViewBag.Models = models;
            ViewBag.Colors = colors;
            ViewBag.CurrentMonth = DateTime.Now.Month;
            ViewBag.CurrentYear = DateTime.Now.Year;
            return View();
        }

        // GET: Commissions/ValidateChassis (AJAX — subdealer chassis check)
        [AuthorizeRole(2)]
        public async Task<IActionResult> ValidateChassis(string chassisNumber, int? modelId, int? colorId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return Unauthorized();

            var chassis = chassisNumber?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(chassis))
                return Json(new { success = false, message = "Enter chassis number." });

            var vehicles = await _mediator.Send(new GetVehiclesQuery { SubdealerId = userId.Value });
            var vehicle = vehicles.FirstOrDefault(v =>
                string.Equals(v.ChassisNumber?.Trim(), chassis, StringComparison.OrdinalIgnoreCase));

            if (vehicle == null)
                return Json(new { success = false, message = "Chassis not found or not allocated to your account." });

            if (modelId.HasValue && vehicle.ModelId != modelId.Value)
                return Json(new { success = false, message = $"Chassis belongs to model {vehicle.ModelName}, not the selected model." });

            if (colorId.HasValue && vehicle.ColorId != colorId.Value)
                return Json(new { success = false, message = $"Chassis belongs to color {vehicle.ColorName}, not the selected color." });

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
            if (booking == null || !booking.InvoiceDate.HasValue)
                return Json(new { success = false, message = "Vehicle must be invoiced by the dealer before commission can be submitted." });

            return Json(new
            {
                success = true,
                message = "Chassis validated. Vehicle invoiced on " + booking.InvoiceDate.Value.ToString("yyyy-MM-dd") + ".",
                vehicleId = vehicle.VehicleId,
                modelName = vehicle.ModelName,
                colorName = vehicle.ColorName,
                invoiceDate = booking.InvoiceDate.Value.ToString("yyyy-MM-dd")
            });
        }

        // GET: Commissions/GetRate?modelId=1&month=1&year=2026 (AJAX)
        [AuthorizeRole(2)]
        public async Task<IActionResult> GetRate(int modelId, int month, int year)
        {
            var rates = await _mediator.Send(new GetCommissionRatesQuery { ModelId = modelId });
            var rate = rates.FirstOrDefault(r => r.IsEffectiveForMonthYear(month, year));

            if (rate == null)
                return Json(new { success = false, message = "No commission rate found for this model and month." });

            return Json(new { success = true, amount = rate.CommissionAmount });
        }

        // POST: Commissions/Submit (Subdealer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.CommissionSubmit)]
        public async Task<IActionResult> Submit(int modelId, int colorId, string chassisNumber,
            int month, int year, decimal commissionAmount)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(chassisNumber))
            {
                TempData["Error"] = "Chassis number is required.";
                return RedirectToAction(nameof(Submit));
            }

            try
            {
                await _mediator.Send(new SubmitCommissionCommand
                {
                    SubdealerId = userId.Value,
                    ChassisNumber = chassisNumber.Trim(),
                    ModelId = modelId,
                    ColorId = colorId,
                    Month = month,
                    Year = year,
                    CommissionAmount = commissionAmount,
                    SubmittedBy = userId.Value
                });

                TempData["Success"] = $"Commission of ₹{commissionAmount:N2} submitted successfully for chassis {chassisNumber}!";
                return RedirectToAction(nameof(MyCommissions));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Submit));
            }
        }

        // GET: Commissions/Approvals (Admin — review submitted commissions)
        [AuthorizeRole(1)]
        public async Task<IActionResult> Approvals(int? status, int? subdealerId, DateTime? fromDate, DateTime? toDate, int? page)
        {
            if (!Request.Query.ContainsKey("status"))
                status = 0;

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var commissions = await _mediator.Send(new GetCommissionsQuery
            {
                Status = status,
                SubdealerId = subdealerId,
                FromDate = from,
                ToDate = to
            });

            var commissionList = commissions.ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(commissionList, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            var subdealers = (await _mediator.Send(new GetSubdealersQuery { IsActive = true })).ToList();
            ViewBag.Subdealers = subdealers;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSubdealerId = subdealerId;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.PendingCount = commissionList.Count(c => c.CanBeApproved());
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Commission);

            return View(pageItems);
        }

        [AuthorizeRole(1)]
        public async Task<IActionResult> ExportApprovals(int? status, int? subdealerId, DateTime? fromDate, DateTime? toDate)
        {
            if (!Request.Query.ContainsKey("status"))
                status = 0;

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var commissions = (await _mediator.Send(new GetCommissionsQuery
            {
                Status = status,
                SubdealerId = subdealerId,
                FromDate = from,
                ToDate = to
            })).ToList();

            var headers = new[] { "ID", "Subdealer", "Chassis", "Month", "Year", "Amount", "Status", "Submitted", "Approved", "Rejected", "Remarks" };
            var rows = commissions.Select(c => (IReadOnlyList<object?>)new List<object?>
            {
                c.CommissionId, c.SubdealerName, c.VehicleChassisNumber,
                c.Month, c.Year, c.CommissionAmount, c.GetStatusDisplay(),
                c.CreatedDate, c.ApprovedDate, c.RejectedDate, c.Notes ?? ""
            });
            return ExcelExportHelper.ToFileResult(this, $"commission_approvals_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Commissions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> Approve(int id, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                var result = await _mediator.Send(new ApproveCommissionCommand
                {
                    CommissionId = id,
                    ApprovedBy = userId.Value,
                    Remarks = string.IsNullOrWhiteSpace(remarks) ? "Approved" : remarks.Trim()
                });

                TempData[result ? "Success" : "Error"] = result
                    ? "Commission approved and credited to subdealer account."
                    : "Commission could not be approved (not found or not pending).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Approvals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> Reject(int id, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                var result = await _mediator.Send(new RejectCommissionCommand
                {
                    CommissionId = id,
                    RejectedBy = userId.Value,
                    Remarks = remarks?.Trim() ?? ""
                });

                TempData[result ? "Success" : "Error"] = result
                    ? "Commission rejected."
                    : "Commission could not be rejected (not found or not pending).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Approvals));
        }

        // GET: Commissions/MyCommissions (Subdealer)
        [AuthorizeRole(2)]
        public async Task<IActionResult> MyCommissions()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var commissions = await _mediator.Send(new GetCommissionsQuery { SubdealerId = userId.Value });
            return View(commissions);
        }
    }
}

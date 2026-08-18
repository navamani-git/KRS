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
        private readonly ICommissionRateService _commissionRates;

        public CommissionsController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            IStatusLookupService statuses,
            ICommissionRateService commissionRates)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _statuses = statuses;
            _commissionRates = commissionRates;
        }

        // GET: Commissions (Admin - Commission Rates)
        [AuthorizeRole(1)]
        public async Task<IActionResult> Index(int? modelId, bool? activeOnly, DateTime? effectiveFrom, DateTime? effectiveTo, int? page)
        {
            var now = DateTime.Now;
            var filterFrom = effectiveFrom?.Date ?? new DateTime(now.Year, now.Month, 1);
            var filterTo = effectiveTo?.Date ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            var rates = await _mediator.Send(new GetCommissionRatesQuery
            {
                ModelId = modelId,
                ActiveOnly = activeOnly,
                EffectiveFrom = filterFrom,
                EffectiveTo = filterTo
            });

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(rates, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.Models = models;
            ViewBag.SelectedModelId = modelId;
            ViewBag.ActiveOnly = activeOnly;
            ViewBag.EffectiveFrom = filterFrom.ToString("yyyy-MM-dd");
            ViewBag.EffectiveTo = filterTo.ToString("yyyy-MM-dd");

            return View(pageItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> CarryForward()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                var count = await _mediator.Send(new CarryForwardCommissionRatesCommand
                {
                    CreatedBy = userId.Value
                });

                TempData[count > 0 ? "Success" : "Info"] = count > 0
                    ? $"Carried forward {count} commission rate(s) for the current month."
                    : "No models needed carry-forward — current month already has rates for all applicable models.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [AuthorizeRole(1)]
        public async Task<IActionResult> Export(int? modelId, bool? activeOnly, DateTime? effectiveFrom, DateTime? effectiveTo)
        {
            var now = DateTime.Now;
            var filterFrom = effectiveFrom?.Date ?? new DateTime(now.Year, now.Month, 1);
            var filterTo = effectiveTo?.Date ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            var rates = (await _mediator.Send(new GetCommissionRatesQuery
            {
                ModelId = modelId,
                ActiveOnly = activeOnly,
                EffectiveFrom = filterFrom,
                EffectiveTo = filterTo
            })).ToList();
            var headers = new[] { "Model", "Amount", "Effective From", "Effective To", "Notes", "Created" };
            var rows = rates.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.ModelName, r.CommissionAmount,
                r.EffectiveFrom.ToString("yyyy-MM-dd"),
                r.EffectiveTo.ToString("yyyy-MM-dd"),
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
            var now = DateTime.Now;
            ViewBag.EffectiveFrom = new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
            ViewBag.EffectiveTo = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).ToString("yyyy-MM-dd");
            return View();
        }

        // POST: Commissions/CreateRate (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> CreateRate(int modelId, decimal commissionAmount,
            DateTime effectiveFrom, DateTime effectiveTo, string notes)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (commissionAmount <= 0)
            {
                TempData["Error"] = "Commission amount must be greater than zero.";
                return RedirectToAction(nameof(CreateRate));
            }

            if (effectiveTo.Date < effectiveFrom.Date)
            {
                TempData["Error"] = "Effective to must be on or after effective from.";
                return RedirectToAction(nameof(CreateRate));
            }

            try
            {
                await _mediator.Send(new CreateCommissionRateCommand
                {
                    ModelId = modelId,
                    CommissionAmount = commissionAmount,
                    EffectiveFrom = effectiveFrom.Date,
                    EffectiveTo = effectiveTo.Date,
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
        public async Task<IActionResult> EditRate(int id, decimal commissionAmount,
            DateTime effectiveFrom, DateTime effectiveTo, string? notes)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (commissionAmount <= 0)
            {
                TempData["Error"] = "Commission amount must be greater than zero.";
                return RedirectToAction(nameof(EditRate), new { id });
            }

            if (effectiveTo.Date < effectiveFrom.Date)
            {
                TempData["Error"] = "Effective to must be on or after effective from.";
                return RedirectToAction(nameof(EditRate), new { id });
            }

            try
            {
                var result = await _mediator.Send(new UpdateCommissionRateCommand
                {
                    CommissionRateId = id,
                    CommissionAmount = commissionAmount,
                    EffectiveFrom = effectiveFrom.Date,
                    EffectiveTo = effectiveTo.Date,
                    Notes = notes?.Trim(),
                    ModifiedBy = userId.Value
                });

                TempData[result ? "Success" : "Error"] = result
                    ? "Commission rate updated."
                    : "Commission rate not found.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

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

            row.EffectiveTo = DateTime.UtcNow.Date;
            if (row.EffectiveTo < row.EffectiveFrom.Date)
                row.EffectiveTo = row.EffectiveFrom.Date;
            row.ExpiryMonth = row.EffectiveTo.Month;
            row.ExpiryYear = row.EffectiveTo.Year;
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
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            var pending = await _mediator.Send(new GetCommissionPreviewQuery
            {
                SubdealerId = userId.Value,
                PendingOnly = true
            });

            var rates = await _mediator.Send(new GetCommissionRatesQuery
            {
                EffectiveFrom = monthStart,
                EffectiveTo = monthEnd,
                ActiveOnly = true
            });

            ViewBag.Pending = pending;
            ViewBag.CommissionRates = rates;
            ViewBag.RatePeriodLabel = $"{monthStart:MMMM yyyy}";

            return View(pending);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenuAny(MenuKeys.CommissionInvoiced, MenuKeys.CommissionSubmit)]
        public async Task<IActionResult> InvoicedVehicles()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var rows = await _mediator.Send(new GetCommissionPreviewQuery
            {
                SubdealerId = userId.Value
            });

            return View(rows);
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

            if (!booking.RegistrationDate.HasValue)
                return Json(new { success = false, message = "RTO registration date must be recorded by the dealer before commission can be submitted." });

            var invoiceDate = booking.InvoiceDate.Value.Date;
            var rate = await _commissionRates.GetRateAsOfAsync(vehicle.ModelId, invoiceDate);
            if (rate == null)
                return Json(new
                {
                    success = false,
                    message = $"No commission rate found for {vehicle.ModelName} effective on {invoiceDate:yyyy-MM-dd}."
                });

            return Json(new
            {
                success = true,
                message = $"Chassis validated. Invoice date: {invoiceDate:yyyy-MM-dd}.",
                vehicleId = vehicle.VehicleId,
                modelName = vehicle.ModelName,
                colorName = vehicle.ColorName,
                invoiceDate = invoiceDate.ToString("yyyy-MM-dd"),
                month = invoiceDate.Month,
                year = invoiceDate.Year,
                amount = rate.CommissionAmount
            });
        }

        // GET: Commissions/GetRate?modelId=1&invoiceDate=2026-01-10 (AJAX)
        [AuthorizeRole(2)]
        public async Task<IActionResult> GetRate(int modelId, DateTime? invoiceDate, int? month, int? year)
        {
            if (invoiceDate.HasValue)
            {
                var rate = await _commissionRates.GetRateAsOfAsync(modelId, invoiceDate.Value);
                if (rate == null)
                    return Json(new { success = false, message = $"No commission rate found for invoice date {invoiceDate:yyyy-MM-dd}." });

                return Json(new { success = true, amount = rate.CommissionAmount });
            }

            if (!month.HasValue || !year.HasValue)
                return Json(new { success = false, message = "Invoice date or month/year is required." });

            var rates = await _mediator.Send(new GetCommissionRatesQuery { ModelId = modelId });
            var legacyRate = rates.FirstOrDefault(r => r.IsEffectiveForMonthYear(month.Value, year.Value));

            if (legacyRate == null)
                return Json(new { success = false, message = "No commission rate found for this model and month." });

            return Json(new { success = true, amount = legacyRate.CommissionAmount });
        }

        // GET: Commissions/Preview?fromDate&toDate (AJAX — cross-verification grid)
        [AuthorizeRole(2)]
        public async Task<IActionResult> Preview(DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return Unauthorized();

            var from = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate?.Date ?? DateTime.Now.Date;

            var rows = await _mediator.Send(new GetCommissionPreviewQuery
            {
                SubdealerId = userId.Value,
                FromDate = from,
                ToDate = to
            });

            return Json(rows.Select(r => new
            {
                r.ChassisNumber,
                r.ModelName,
                r.ColorName,
                invoiceDate = r.InvoiceDate.ToString("yyyy-MM-dd"),
                r.Month,
                r.Year,
                applicableRate = r.ApplicableRate,
                r.CommissionStatus,
                submittedAmount = r.SubmittedAmount
            }));
        }

        // POST: Commissions/SubmitRow (Subdealer — submit one vehicle from grid)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.CommissionSubmit)]
        public async Task<IActionResult> SubmitRow(int vehicleId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null || vehicle.SubdealerId != userId.Value)
            {
                TempData["Error"] = "Vehicle not found or not allocated to your account.";
                return RedirectToAction(nameof(Submit));
            }

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == vehicleId);
            if (booking?.InvoiceDate == null)
            {
                TempData["Error"] = "Vehicle must be invoiced before commission can be submitted.";
                return RedirectToAction(nameof(Submit));
            }

            if (booking.RegistrationDate == null)
            {
                TempData["Error"] = "RTO registration date must be recorded before commission can be submitted.";
                return RedirectToAction(nameof(Submit));
            }

            var invoice = booking.InvoiceDate.Value.Date;
            var rate = await _commissionRates.GetRateAsOfAsync(vehicle.ModelId, invoice);
            if (rate == null)
            {
                TempData["Error"] = $"No commission rate configured for invoice date {invoice:yyyy-MM-dd}.";
                return RedirectToAction(nameof(Submit));
            }

            try
            {
                await _mediator.Send(new SubmitCommissionCommand
                {
                    SubdealerId = userId.Value,
                    ChassisNumber = vehicle.ChassisNumber ?? "",
                    ModelId = vehicle.ModelId,
                    ColorId = vehicle.ColorId,
                    Month = invoice.Month,
                    Year = invoice.Year,
                    CommissionAmount = rate.CommissionAmount,
                    SubmittedBy = userId.Value
                });

                TempData["Success"] = $"Commission of ₹{rate.CommissionAmount:N2} submitted for chassis {vehicle.ChassisNumber}. Pending admin approval.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Submit));
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
        [AuthorizeMenuAny(MenuKeys.CommissionSubmit, MenuKeys.CommissionView)]
        public async Task<IActionResult> MyCommissions()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var commissions = await _mediator.Send(new GetCommissionsQuery { SubdealerId = userId.Value });
            return View(commissions);
        }
    }
}

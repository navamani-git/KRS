using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)] // Admin only
    public class PricesController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public PricesController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int? modelId, int? colorId, int? month, int? year, int? page, int? pageSize)
        {
            if (!Request.Query.ContainsKey("month") && !Request.Query.ContainsKey("year"))
            {
                return RedirectToAction(nameof(Index), new
                {
                    modelId,
                    colorId,
                    month = DateTime.Now.Month,
                    year = DateTime.Now.Year,
                    page,
                    pageSize
                });
            }

            var filterYear = year ?? DateTime.Now.Year;
            var prices = await _mediator.Send(new GetVehiclePricesQuery
            {
                ModelId = modelId,
                ColorId = colorId,
                Month = month,
                Year = filterYear
            });

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var colors = await _mediator.Send(new GetVehicleColorsQuery { IsActive = true });

            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Prices);
            prices = GridScreenFilterHelper.ApplyPrices(prices, columnFilters);

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(prices, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.Models = models;
            ViewBag.Colors = colors;
            ViewBag.SelectedModelId = modelId;
            ViewBag.SelectedColorId = colorId;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = filterYear;

            return View(pageItems);
        }

        public async Task<IActionResult> Create(int? modelId)
        {
            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });

            ViewBag.Models = models;
            ViewBag.SelectedModelId = modelId;
            ViewBag.CurrentMonth = DateTime.Now.Month;
            ViewBag.CurrentYear = DateTime.Now.Year;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AvailableColors(
            int modelId,
            int month,
            int year,
            DateTime? effectiveFrom,
            DateTime? effectiveTo)
        {
            if (modelId <= 0)
                return Json(Array.Empty<object>());

            var mappedColorIds = (await _unitOfWork.VehicleModelColors.GetColorIdsByModelIdAsync(modelId)).ToList();
            if (mappedColorIds.Count == 0)
                return Json(Array.Empty<object>());

            var from = effectiveFrom?.Date ?? new DateTime(year, month, 1);
            var to = effectiveTo?.Date ?? default;
            if (to == default)
                to = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var existing = (await _unitOfWork.VehiclePriceHistories.GetAllAsync()).ToList();
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync())
                .Where(c => c.IsActive && mappedColorIds.Contains(c.ColorId))
                .OrderBy(c => c.ColorName)
                .ToList();

            var available = new List<object>();
            foreach (var color in colors)
            {
                if (VehiclePriceOverlapHelper.TryFindOverlap(
                        existing, modelId, color.ColorId, from, to, excludePriceHistoryId: null, out _))
                    continue;

                available.Add(new
                {
                    id = color.ColorId,
                    name = color.ColorName,
                    hex = color.HexCode
                });
            }

            return Json(available);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int modelId,
            int[]? colorIds,
            bool applyForAllColors,
            int month,
            int year,
            decimal price,
            string? notes,
            DateTime effectiveFrom,
            DateTime effectiveTo)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (price <= 0)
            {
                TempData["Error"] = "Price must be greater than zero.";
                return RedirectToAction(nameof(Create));
            }

            var selectedColorIds = colorIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (!applyForAllColors && selectedColorIds.Count == 0)
            {
                TempData["Error"] = "Select at least one color.";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                await _mediator.Send(new CreateVehiclePriceCommand
                {
                    ModelId = modelId,
                    ColorIds = selectedColorIds,
                    ApplyForAllColors = applyForAllColors,
                    Month = month,
                    Year = year,
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = effectiveTo,
                    Price = price,
                    Notes = notes?.Trim(),
                    CreatedBy = userId.Value
                });

                var toLabel = effectiveTo == default ? "month end" : effectiveTo.ToString("yyyy-MM-dd");
                TempData["Success"] = applyForAllColors
                    ? $"Price ₹{price:N2} saved for all mapped colors ({effectiveFrom:yyyy-MM-dd} to {toLabel})."
                    : selectedColorIds.Count == 1
                        ? $"Price ₹{price:N2} saved for 1 color ({effectiveFrom:yyyy-MM-dd} to {toLabel})."
                        : $"Price ₹{price:N2} saved for {selectedColorIds.Count} colors ({effectiveFrom:yyyy-MM-dd} to {toLabel}).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }

        public async Task<IActionResult> Export(int? modelId, int? colorId, int? month, int? year)
        {
            var prices = await _mediator.Send(new GetVehiclePricesQuery
            {
                ModelId = modelId,
                ColorId = colorId,
                Month = month,
                Year = year ?? DateTime.Now.Year
            });
            var headers = new[] { "Model", "Color", "Month", "Year", "Effective From", "Effective To", "Price", "Notes" };
            var rows = prices.Select(p => (IReadOnlyList<object?>)new List<object?>
            {
                p.ModelName, p.ColorName, p.Month, p.Year,
                p.EffectiveFrom.ToString("yyyy-MM-dd"), p.EffectiveTo.ToString("yyyy-MM-dd"),
                p.Price, p.Notes ?? ""
            });
            return ExcelExportHelper.ToFileResult(this, $"prices_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Prices");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.VehiclePriceHistories.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Price record not found.";
                return RedirectToAction(nameof(Index));
            }

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var colors = await _mediator.Send(new GetVehicleColorsQuery { IsActive = true });
            ViewBag.Models = models;
            ViewBag.Colors = colors;
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            decimal price,
            string? notes,
            DateTime effectiveFrom,
            DateTime effectiveTo,
            string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (price <= 0)
            {
                TempData["Error"] = "Price must be greater than zero.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            try
            {
                var ok = await _mediator.Send(new UpdateVehiclePriceCommand
                {
                    PriceHistoryId = id,
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = effectiveTo,
                    Price = price,
                    Notes = notes?.Trim(),
                    ModifiedBy = userId.Value,
                    Remarks = remarks?.Trim()
                });

                TempData[ok ? "Success" : "Error"] = ok ? "Price updated." : "Price record not found.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.VehiclePriceHistories.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Price record not found.";
                return RedirectToAction(nameof(Index));
            }

            await _unitOfWork.VehiclePriceHistories.DeleteAsync(id);
            TempData["Success"] = "Price record deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}

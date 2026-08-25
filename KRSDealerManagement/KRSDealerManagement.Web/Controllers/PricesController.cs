using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
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
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int modelId,
            int colorId,
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

            if (!applyForAllColors && colorId <= 0)
            {
                TempData["Error"] = "Select a color or check apply for all colors.";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                await _mediator.Send(new CreateVehiclePriceCommand
                {
                    ModelId = modelId,
                    ColorId = colorId,
                    ApplyForAllColors = applyForAllColors,
                    Month = month,
                    Year = year,
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = effectiveTo,
                    Price = price,
                    Notes = notes?.Trim(),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = applyForAllColors
                    ? $"Price ₹{price:N2} saved for all mapped colors ({effectiveFrom:yyyy-MM-dd} to {(effectiveTo == default ? "month end" : effectiveTo.ToString("yyyy-MM-dd"))})."
                    : $"Price ₹{price:N2} effective {effectiveFrom:yyyy-MM-dd} to {(effectiveTo == default ? "month end" : effectiveTo.ToString("yyyy-MM-dd"))} saved successfully!";
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

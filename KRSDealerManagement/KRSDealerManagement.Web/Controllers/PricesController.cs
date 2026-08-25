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
            // Load prices with filters
            var prices = await _mediator.Send(new GetVehiclePricesQuery
            {
                ModelId = modelId,
                ColorId = colorId,
                Month = month,
                Year = year ?? DateTime.Now.Year
            });

            // Load models and colors for filter dropdowns
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
            ViewBag.SelectedYear = year ?? DateTime.Now.Year;

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
        public async Task<IActionResult> Create(int modelId, int colorId, int month, int year, decimal price, string notes, DateTime effectiveFrom)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (price <= 0)
            {
                TempData["Error"] = "Price must be greater than zero.";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                await _mediator.Send(new CreateVehiclePriceCommand
                {
                    ModelId = modelId,
                    ColorId = colorId,
                    Month = month,
                    Year = year,
                    EffectiveFrom = effectiveFrom,
                    Price = price,
                    Notes = notes?.Trim(),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Price ₹{price:N2} effective {effectiveFrom:yyyy-MM-dd} saved successfully!";
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
            var headers = new[] { "Model", "Color", "Month", "Year", "Effective From", "Price", "Notes" };
            var rows = prices.Select(p => (IReadOnlyList<object?>)new List<object?>
            {
                p.ModelName, p.ColorName, p.Month, p.Year, p.EffectiveFrom.ToString("yyyy-MM-dd"), p.Price, p.Notes ?? ""
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
        public async Task<IActionResult> Edit(int id, decimal price, string? notes)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var row = await _unitOfWork.VehiclePriceHistories.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Price record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (price <= 0)
            {
                TempData["Error"] = "Price must be greater than zero.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            row.Price = price;
            row.Notes = notes?.Trim();
            row.ModifiedBy = userId.Value;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.VehiclePriceHistories.UpdateAsync(row);
            TempData["Success"] = "Price updated.";
            return RedirectToAction(nameof(Index));
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

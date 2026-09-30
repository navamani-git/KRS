using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeMenu(StaffMenuAccess.GstRates)]
    public class GstRatesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public GstRatesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index()
        {
            var rates = (await _unitOfWork.GstRates.GetAllAsync())
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.SortOrder)
                .ThenBy(r => r.RatePercent)
                .ToList();

            var defaults = await _unitOfWork.GstScreenDefaults.GetRateIdByScreenKeyAsync();
            ViewBag.ScreenDefaults = defaults;
            ViewBag.ScreenDefinitions = GstScreenKeys.All;
            ViewBag.ActiveRates = rates.Where(r => r.IsActive).ToList();

            return View(rates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScreenDefaults(IFormCollection form)
        {
            var activeIds = (await _unitOfWork.GstRates.GetAllAsync())
                .Where(r => r.IsActive)
                .Select(r => r.GstRateId)
                .ToHashSet();

            foreach (var screen in GstScreenKeys.All)
            {
                var raw = form[$"default_{screen.Key}"].ToString();
                if (!int.TryParse(raw, out var gstRateId) || !activeIds.Contains(gstRateId))
                {
                    TempData["Error"] = $"Choose a valid GST rate for {screen.Label}.";
                    return RedirectToAction(nameof(Index));
                }

                await _unitOfWork.GstScreenDefaults.UpsertAsync(screen.Key, gstRateId);
            }

            TempData["Success"] = "Screen GST defaults saved.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal ratePercent, int sortOrder)
        {
            if (ratePercent <= 0 || ratePercent > 100)
            {
                TempData["Error"] = "GST % must be between 0.01 and 100.";
                return View();
            }

            var all = (await _unitOfWork.GstRates.GetAllAsync()).ToList();
            if (all.Any(r => r.RatePercent == ratePercent))
            {
                TempData["Error"] = "This GST % already exists.";
                return View();
            }

            await _unitOfWork.GstRates.AddAsync(new GstRate
            {
                RatePercent = ratePercent,
                SortOrder = sortOrder > 0 ? sortOrder : all.Count + 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            TempData["Success"] = $"GST {ratePercent}% added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.GstRates.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal ratePercent, int sortOrder, bool isActive)
        {
            var row = await _unitOfWork.GstRates.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            if (ratePercent <= 0 || ratePercent > 100)
            {
                TempData["Error"] = "GST % must be between 0.01 and 100.";
                return View(row);
            }

            if ((await _unitOfWork.GstRates.GetAllAsync()).Any(r => r.GstRateId != id && r.RatePercent == ratePercent))
            {
                TempData["Error"] = "This GST % already exists.";
                return View(row);
            }

            row.RatePercent = ratePercent;
            row.SortOrder = sortOrder;
            row.IsActive = isActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.GstRates.UpdateAsync(row);

            TempData["Success"] = "GST rate updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var row = await _unitOfWork.GstRates.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            row.IsActive = !row.IsActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.GstRates.UpdateAsync(row);
            TempData["Success"] = $"GST rate marked {(row.IsActive ? "active" : "inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Export()
        {
            var list = (await _unitOfWork.GstRates.GetAllAsync())
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.RatePercent)
                .ToList();
            var headers = new[] { "GST %", "Sort Order", "Status", "Created" };
            var rows = list.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.RatePercent,
                r.SortOrder,
                r.IsActive ? "Active" : "Inactive",
                r.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"gst_rates_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "GST Rates");
        }
    }
}

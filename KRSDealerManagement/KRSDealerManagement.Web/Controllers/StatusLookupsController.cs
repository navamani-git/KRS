using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Services;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.StatusLookups)]
    public class StatusLookupsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;
        private readonly IQueryStringCrypto _queryCrypto;

        public StatusLookupsController(IUnitOfWork unitOfWork, IStatusLookupService statuses, IQueryStringCrypto queryCrypto)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
            _queryCrypto = queryCrypto;
        }

        public async Task<IActionResult> Index(string? category, int? page)
        {
            var list = (await _statuses.GetAllByCategoryAsync(category)).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = StatusCategories.All;
            return View(pageItems);
        }

        public async Task<IActionResult> Export(string? category)
        {
            var list = (await _statuses.GetAllByCategoryAsync(category)).ToList();
            var headers = new[] { "Category", "Value", "Code", "Name", "Sort Order", "Status" };
            var rows = list.Select(s => (IReadOnlyList<object?>)new List<object?>
            {
                StatusCategories.GetDisplayName(s.Category), s.StatusValue, s.StatusCode, s.StatusName,
                s.SortOrder, s.IsActive ? "Active" : "Inactive"
            });
            return ExcelExportHelper.ToFileResult(this, $"status_lookups_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Statuses");
        }

        public IActionResult Create(string? category)
        {
            ViewBag.Categories = StatusCategories.All;
            ViewBag.BadgeOptions = StatusBadgeOptions.All;
            ViewBag.SelectedCategory = category;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string category,
            int statusValue,
            string statusCode,
            string statusName,
            string badgeClass,
            int sortOrder)
        {
            ViewBag.Categories = StatusCategories.All;
            ViewBag.BadgeOptions = StatusBadgeOptions.All;
            ViewBag.SelectedCategory = category;

            if (!StatusCategories.IsValid(category))
            {
                TempData["Error"] = "Please select a valid category.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(statusCode) || string.IsNullOrWhiteSpace(statusName))
            {
                TempData["Error"] = "Status code and name are required.";
                return View();
            }

            var code = statusCode.Trim().ToUpperInvariant();
            var name = statusName.Trim();
            var badge = string.IsNullOrWhiteSpace(badgeClass) ? "bg-secondary" : badgeClass.Trim();

            var all = (await _unitOfWork.StatusLookups.GetAllAsync()).ToList();
            if (all.Any(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                             && s.StatusValue == statusValue))
            {
                TempData["Error"] = $"Status value {statusValue} already exists for {StatusCategories.GetDisplayName(category)}.";
                return View();
            }

            if (all.Any(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                             && s.StatusCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = $"Status code '{code}' already exists for this category.";
                return View();
            }

            await _unitOfWork.StatusLookups.AddAsync(new StatusLookup
            {
                Category = category.ToUpperInvariant(),
                StatusValue = statusValue,
                StatusCode = code,
                StatusName = name,
                BadgeClass = badge,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });

            _statuses.InvalidateCache();
            TempData["Success"] = $"Status '{name}' added.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { category }));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.StatusLookups.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Status not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BadgeOptions = StatusBadgeOptions.All;
            ViewBag.CategoryDisplay = StatusCategories.GetDisplayName(row.Category);
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string statusName,
            string badgeClass,
            int sortOrder,
            bool isActive)
        {
            var row = await _unitOfWork.StatusLookups.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Status not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(statusName))
            {
                TempData["Error"] = "Status name is required.";
                return this.RedirectEncrypted(nameof(Edit), new { id });
            }

            row.StatusName = statusName.Trim();
            row.BadgeClass = string.IsNullOrWhiteSpace(badgeClass) ? "bg-secondary" : badgeClass.Trim();
            row.SortOrder = sortOrder;
            row.IsActive = isActive;

            await _unitOfWork.StatusLookups.UpdateAsync(row);
            _statuses.InvalidateCache();

            TempData["Success"] = $"Status '{row.StatusName}' updated.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { category = row.Category }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var row = await _unitOfWork.StatusLookups.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Status not found.";
                return RedirectToAction(nameof(Index));
            }

            row.IsActive = !row.IsActive;
            await _unitOfWork.StatusLookups.UpdateAsync(row);
            _statuses.InvalidateCache();

            TempData["Success"] = $"'{row.StatusName}' marked {(row.IsActive ? "active" : "inactive")}.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { category = row.Category }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.StatusLookups.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Status not found.";
                return RedirectToAction(nameof(Index));
            }

            row.IsActive = false;
            await _unitOfWork.StatusLookups.UpdateAsync(row);
            _statuses.InvalidateCache();

            TempData["Success"] = $"'{row.StatusName}' deactivated.";
            return new RedirectResult(QueryStringUrlHelper.EncryptedAction(Url, _queryCrypto, nameof(Index), new { category = row.Category }));
        }
    }
}

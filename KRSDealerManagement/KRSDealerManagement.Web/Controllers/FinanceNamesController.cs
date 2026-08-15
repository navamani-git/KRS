using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.FinanceNames)]
    public class FinanceNamesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public FinanceNamesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index(int? page)
        {
            var list = (await _unitOfWork.FinanceNames.GetAllAsync())
                .OrderByDescending(f => f.IsActive)
                .ThenBy(f => f.FinanceName)
                .ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public async Task<IActionResult> Export()
        {
            var list = (await _unitOfWork.FinanceNames.GetAllAsync())
                .OrderByDescending(f => f.IsActive)
                .ThenBy(f => f.FinanceName)
                .ToList();
            var headers = new[] { "Finance Name", "Status", "Created" };
            var rows = list.Select(f => (IReadOnlyList<object?>)new List<object?>
            {
                f.FinanceName, f.IsActive ? "Active" : "Inactive", f.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"finance_names_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Finance Names");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string financeName)
        {
            if (string.IsNullOrWhiteSpace(financeName))
            {
                TempData["Error"] = "Finance name is required.";
                return View();
            }

            var name = financeName.Trim().ToUpperInvariant();
            var exists = (await _unitOfWork.FinanceNames.GetAllAsync())
                .Any(f => f.FinanceName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                TempData["Error"] = "This finance name already exists.";
                return View();
            }

            await _unitOfWork.FinanceNames.AddAsync(new FinanceNameMaster
            {
                FinanceName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            TempData["Success"] = $"Finance name '{name}' added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var row = await _unitOfWork.FinanceNames.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            row.IsActive = !row.IsActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.FinanceNames.UpdateAsync(row);
            TempData["Success"] = $"Finance name marked {(row.IsActive ? "active" : "inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.FinanceNames.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string financeName, bool isActive)
        {
            var row = await _unitOfWork.FinanceNames.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(financeName)) { TempData["Error"] = "Name required."; return View(row); }

            var name = financeName.Trim().ToUpperInvariant();
            if ((await _unitOfWork.FinanceNames.GetAllAsync()).Any(f => f.FinanceNameId != id && f.FinanceName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Name already exists."; return View(row); }

            row.FinanceName = name;
            row.IsActive = isActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.FinanceNames.UpdateAsync(row);
            TempData["Success"] = "Finance name updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.FinanceNames.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            row.IsActive = false;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.FinanceNames.UpdateAsync(row);
            TempData["Success"] = "Finance name deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

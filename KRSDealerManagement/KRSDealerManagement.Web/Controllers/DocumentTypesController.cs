using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.DocumentTypes)]
    public class DocumentTypesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public DocumentTypesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index(int? page, int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.DocumentTypes);
            var list = GridScreenFilterHelper.ApplyDocumentTypes(
                (await _unitOfWork.DocumentTypes.GetAllAsync()).OrderByDescending(d => d.IsActive).ThenBy(d => d.TypeName),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public async Task<IActionResult> Export()
        {
            var list = (await _unitOfWork.DocumentTypes.GetAllAsync()).OrderByDescending(d => d.IsActive).ThenBy(d => d.TypeName).ToList();
            var headers = new[] { "Type Name", "Status", "Created" };
            var rows = list.Select(d => (IReadOnlyList<object?>)new List<object?>
            {
                d.TypeName, d.IsActive ? "Active" : "Inactive", d.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"document_types_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Document Types");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) { TempData["Error"] = "Name required."; return View(); }
            var name = typeName.Trim();
            if ((await _unitOfWork.DocumentTypes.GetAllAsync()).Any(d => d.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Already exists."; return View(); }
            await _unitOfWork.DocumentTypes.AddAsync(new DocumentTypeMaster { TypeName = name, IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            TempData["Success"] = "Document type added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var row = await _unitOfWork.DocumentTypes.GetByIdAsync(id);
            if (row == null) return RedirectToAction(nameof(Index));
            row.IsActive = !row.IsActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.DocumentTypes.UpdateAsync(row);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.DocumentTypes.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string typeName, bool isActive)
        {
            var row = await _unitOfWork.DocumentTypes.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(typeName)) { TempData["Error"] = "Name required."; return View(row); }

            var name = typeName.Trim();
            if ((await _unitOfWork.DocumentTypes.GetAllAsync()).Any(d => d.DocumentTypeId != id && d.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Already exists."; return View(row); }

            row.TypeName = name;
            row.IsActive = isActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.DocumentTypes.UpdateAsync(row);
            TempData["Success"] = "Document type updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.DocumentTypes.GetByIdAsync(id);
            if (row == null) return RedirectToAction(nameof(Index));
            row.IsActive = false;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.DocumentTypes.UpdateAsync(row);
            TempData["Success"] = "Document type deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

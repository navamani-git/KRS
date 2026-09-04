using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 4)]
    [AuthorizeMenu(StaffMenuAccess.WarrantyParts)]
    public class WarrantyPartsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarrantyPartsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index(int? page, int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.WarrantyParts);
            var list = GridScreenFilterHelper.ApplyWarrantyParts(
                (await _unitOfWork.WarrantyParts.GetAllAsync())
                    .OrderByDescending(p => p.IsActive)
                    .ThenBy(p => p.SortOrder)
                    .ThenBy(p => p.PartName),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string partName, string? partCode)
        {
            if (string.IsNullOrWhiteSpace(partName))
            {
                TempData["Error"] = "Part name is required.";
                return View();
            }

            var name = partName.Trim().ToUpperInvariant();
            if ((await _unitOfWork.WarrantyParts.GetAllAsync()).Any(p => p.PartName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This part name already exists.";
                return View();
            }

            await _unitOfWork.WarrantyParts.AddAsync(new WarrantyPartMaster
            {
                PartName = name,
                PartCode = string.IsNullOrWhiteSpace(partCode) ? null : partCode.Trim().ToUpperInvariant(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Part '{name}' added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var part = await _unitOfWork.WarrantyParts.GetByIdAsync(id);
            if (part == null) return NotFound();
            return View(part);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string partName, string? partCode, bool isActive)
        {
            var part = await _unitOfWork.WarrantyParts.GetByIdAsync(id);
            if (part == null) return NotFound();
            if (string.IsNullOrWhiteSpace(partName))
            {
                TempData["Error"] = "Part name is required.";
                return View(part);
            }

            var name = partName.Trim().ToUpperInvariant();
            if ((await _unitOfWork.WarrantyParts.GetAllAsync()).Any(p => p.WarrantyPartId != id && p.PartName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "Another part with this name already exists.";
                return View(part);
            }

            part.PartName = name;
            part.PartCode = string.IsNullOrWhiteSpace(partCode) ? null : partCode.Trim().ToUpperInvariant();
            part.IsActive = isActive;
            part.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.WarrantyParts.UpdateAsync(part);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Part updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

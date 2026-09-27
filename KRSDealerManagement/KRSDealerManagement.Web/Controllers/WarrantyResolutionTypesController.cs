using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeMenu(StaffMenuAccess.WarrantyParts)]
    public class WarrantyResolutionTypesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarrantyResolutionTypesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index(int? page, int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.WarrantyResolutionTypes);
            var list = GridScreenFilterHelper.ApplyWarrantyResolutionTypes(
                (await _unitOfWork.WarrantyResolutionTypes.GetAllAsync())
                    .OrderByDescending(r => r.IsActive)
                    .ThenBy(r => r.SortOrder)
                    .ThenBy(r => r.Name),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? code, string actionType, int sortOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name is required.";
                return View();
            }
            if (!WarrantyResolutionActionTypes.IsValid(actionType))
            {
                TempData["Error"] = "Action type is required.";
                return View();
            }

            var resolvedCode = string.IsNullOrWhiteSpace(code)
                ? ToCode(name)
                : code.Trim().ToUpperInvariant();
            if ((await _unitOfWork.WarrantyResolutionTypes.GetAllAsync())
                .Any(r => r.Code.Equals(resolvedCode, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This resolution code already exists.";
                return View();
            }

            await _unitOfWork.WarrantyResolutionTypes.AddAsync(new WarrantyResolutionType
            {
                Code = resolvedCode,
                Name = name.Trim(),
                ActionType = WarrantyResolutionActionTypes.Normalize(actionType),
                IsActive = true,
                SortOrder = sortOrder,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Resolution '{name.Trim()}' added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _unitOfWork.WarrantyResolutionTypes.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string actionType, int sortOrder, bool isActive)
        {
            var item = await _unitOfWork.WarrantyResolutionTypes.GetByIdAsync(id);
            if (item == null) return NotFound();
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name is required.";
                return View(item);
            }
            if (!WarrantyResolutionActionTypes.IsValid(actionType))
            {
                TempData["Error"] = "Action type is required.";
                return View(item);
            }

            item.Name = name.Trim();
            item.ActionType = WarrantyResolutionActionTypes.Normalize(actionType);
            item.SortOrder = sortOrder;
            item.IsActive = isActive;
            item.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.WarrantyResolutionTypes.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Resolution type updated.";
            return RedirectToAction(nameof(Index));
        }

        private static string ToCode(string name)
        {
            var chars = name.Trim().ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray();
            var code = new string(chars);
            while (code.Contains("__", StringComparison.Ordinal))
                code = code.Replace("__", "_", StringComparison.Ordinal);
            return code.Trim('_');
        }
    }
}

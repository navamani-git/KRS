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
    [AuthorizeMenu(StaffMenuAccess.RtoLocations)]
    public class RtoLocationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public RtoLocationsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index(int? page, int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.RtoLocations);
            var list = GridScreenFilterHelper.ApplyRtoLocations(
                (await _unitOfWork.RtoLocations.GetAllAsync()).OrderByDescending(r => r.IsActive).ThenBy(r => r.LocationName),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public async Task<IActionResult> Export()
        {
            var list = (await _unitOfWork.RtoLocations.GetAllAsync()).OrderByDescending(r => r.IsActive).ThenBy(r => r.LocationName).ToList();
            var headers = new[] { "Location", "Status", "Created" };
            var rows = list.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.LocationName, r.IsActive ? "Active" : "Inactive", r.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"rto_locations_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "RTO Locations");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName)) { TempData["Error"] = "Name required."; return View(); }
            var name = locationName.Trim();
            if ((await _unitOfWork.RtoLocations.GetAllAsync()).Any(r => r.LocationName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Already exists."; return View(); }
            await _unitOfWork.RtoLocations.AddAsync(new RtoLocationMaster { LocationName = name, IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            TempData["Success"] = "RTO location added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var row = await _unitOfWork.RtoLocations.GetByIdAsync(id);
            if (row == null) return RedirectToAction(nameof(Index));
            row.IsActive = !row.IsActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RtoLocations.UpdateAsync(row);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.RtoLocations.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string locationName, bool isActive)
        {
            var row = await _unitOfWork.RtoLocations.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            if (string.IsNullOrWhiteSpace(locationName)) { TempData["Error"] = "Name required."; return View(row); }

            var name = locationName.Trim();
            if ((await _unitOfWork.RtoLocations.GetAllAsync()).Any(r => r.RtoLocationId != id && r.LocationName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Already exists."; return View(row); }

            row.LocationName = name;
            row.IsActive = isActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RtoLocations.UpdateAsync(row);
            TempData["Success"] = "RTO location updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.RtoLocations.GetByIdAsync(id);
            if (row == null) return RedirectToAction(nameof(Index));
            row.IsActive = false;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RtoLocations.UpdateAsync(row);
            TempData["Success"] = "RTO location deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

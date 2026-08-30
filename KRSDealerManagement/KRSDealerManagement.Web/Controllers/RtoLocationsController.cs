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
            var districts = (await _unitOfWork.RtoDistricts.GetAllAsync()).ToDictionary(d => d.RtoDistrictId, d => d.DistrictName);
            ViewBag.DistrictNames = districts;
            var list = GridScreenFilterHelper.ApplyRtoLocations(
                (await _unitOfWork.RtoLocations.GetAllAsync()).OrderByDescending(r => r.IsActive).ThenBy(r => r.LocationName),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        [HttpGet]
        public async Task<IActionResult> LocationsByDistrict(int districtId)
        {
            var locations = (await _unitOfWork.RtoLocations.GetAllAsync())
                .Where(r => r.IsActive && r.RtoDistrictId == districtId)
                .OrderBy(r => r.LocationName)
                .Select(r => new { r.RtoLocationId, r.LocationName });
            return Json(locations);
        }

        public async Task<IActionResult> Export()
        {
            var districts = (await _unitOfWork.RtoDistricts.GetAllAsync()).ToDictionary(d => d.RtoDistrictId, d => d.DistrictName);
            var list = (await _unitOfWork.RtoLocations.GetAllAsync()).OrderByDescending(r => r.IsActive).ThenBy(r => r.LocationName).ToList();
            var headers = new[] { "District", "Location", "Status", "Created" };
            var rows = list.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                districts.GetValueOrDefault(r.RtoDistrictId, $"#{r.RtoDistrictId}"),
                r.LocationName,
                r.IsActive ? "Active" : "Inactive",
                r.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"rto_locations_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "RTO Locations");
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Districts = (await _unitOfWork.RtoDistricts.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.DistrictName);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int rtoDistrictId, string locationName)
        {
            ViewBag.Districts = (await _unitOfWork.RtoDistricts.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.DistrictName);
            if (rtoDistrictId <= 0) { TempData["Error"] = "District is required."; return View(); }
            if (string.IsNullOrWhiteSpace(locationName)) { TempData["Error"] = "Location name is required."; return View(); }

            var district = await _unitOfWork.RtoDistricts.GetByIdAsync(rtoDistrictId);
            if (district == null || !district.IsActive) { TempData["Error"] = "Selected district is not available."; return View(); }

            var name = locationName.Trim();
            if ((await _unitOfWork.RtoLocations.GetAllAsync()).Any(r => r.LocationName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Location already exists."; return View(); }

            await _unitOfWork.RtoLocations.AddAsync(new RtoLocationMaster
            {
                RtoDistrictId = rtoDistrictId,
                LocationName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
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
            ViewBag.Districts = (await _unitOfWork.RtoDistricts.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.DistrictName);
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int rtoDistrictId, string locationName, bool isActive)
        {
            var row = await _unitOfWork.RtoLocations.GetByIdAsync(id);
            if (row == null) { TempData["Error"] = "Not found."; return RedirectToAction(nameof(Index)); }
            ViewBag.Districts = (await _unitOfWork.RtoDistricts.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.DistrictName);

            if (rtoDistrictId <= 0) { TempData["Error"] = "District is required."; return View(row); }
            if (string.IsNullOrWhiteSpace(locationName)) { TempData["Error"] = "Location name is required."; return View(row); }

            var name = locationName.Trim();
            if ((await _unitOfWork.RtoLocations.GetAllAsync()).Any(r => r.RtoLocationId != id && r.LocationName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"] = "Location already exists."; return View(row); }

            row.RtoDistrictId = rtoDistrictId;
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

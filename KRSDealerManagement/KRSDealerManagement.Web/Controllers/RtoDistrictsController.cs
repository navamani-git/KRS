using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.RtoDistricts)]
    public class RtoDistrictsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public RtoDistrictsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index()
        {
            var districts = (await _unitOfWork.RtoDistricts.GetAllAsync())
                .OrderByDescending(d => d.IsActive)
                .ThenBy(d => d.DistrictName)
                .ToList();
            var locationCounts = (await _unitOfWork.RtoLocations.GetAllAsync())
                .Where(l => l.IsActive)
                .GroupBy(l => l.RtoDistrictId)
                .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.LocationCounts = locationCounts;
            return View(districts);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string districtName)
        {
            if (string.IsNullOrWhiteSpace(districtName))
            {
                TempData["Error"] = "District name is required.";
                return View();
            }

            var name = districtName.Trim();
            if ((await _unitOfWork.RtoDistricts.GetAllAsync()).Any(d => d.DistrictName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "District already exists.";
                return View();
            }

            await _unitOfWork.RtoDistricts.AddAsync(new RtoDistrictMaster
            {
                DistrictName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
            TempData["Success"] = "RTO district added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.RtoDistricts.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "District not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string districtName, bool isActive)
        {
            var row = await _unitOfWork.RtoDistricts.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "District not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(districtName))
            {
                TempData["Error"] = "District name is required.";
                return View(row);
            }

            var name = districtName.Trim();
            if ((await _unitOfWork.RtoDistricts.GetAllAsync()).Any(d => d.RtoDistrictId != id && d.DistrictName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "District already exists.";
                return View(row);
            }

            row.DistrictName = name;
            row.IsActive = isActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RtoDistricts.UpdateAsync(row);
            TempData["Success"] = "RTO district updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var row = await _unitOfWork.RtoDistricts.GetByIdAsync(id);
            if (row == null) return RedirectToAction(nameof(Index));
            row.IsActive = !row.IsActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.RtoDistricts.UpdateAsync(row);
            return RedirectToAction(nameof(Index));
        }
    }
}

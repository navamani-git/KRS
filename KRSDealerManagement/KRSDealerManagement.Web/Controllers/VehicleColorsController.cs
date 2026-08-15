using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)] // Admin only
    public class VehicleColorsController : Controller
    {
        private readonly IMediator _mediator;

        public VehicleColorsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index(string searchTerm, bool? isActive, int? page)
        {
            var colors = await _mediator.Send(new GetVehicleColorsQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive
            });
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(colors, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsActive = isActive;
            return View(pageItems);
        }

        public async Task<IActionResult> Export(string searchTerm, bool? isActive)
        {
            var colors = (await _mediator.Send(new GetVehicleColorsQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive
            })).ToList();
            var headers = new[] { "Color", "Hex Code", "Status", "Created" };
            var rows = colors.Select(c => (IReadOnlyList<object?>)new List<object?>
            {
                c.ColorName, c.HexCode ?? "", c.IsActive ? "Active" : "Inactive", c.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"vehicle_colors_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Colors");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string colorName, string hexCode)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(colorName))
            {
                TempData["Error"] = "Color name is required.";
                return View();
            }

            try
            {
                await _mediator.Send(new CreateVehicleColorCommand
                {
                    ColorName = colorName.Trim(),
                    HexCode = string.IsNullOrWhiteSpace(hexCode) ? null : hexCode.Trim(),
                    CreatedBy = userId.Value
                });
                TempData["Success"] = $"Color '{colorName}' added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var colors = await _mediator.Send(new GetVehicleColorsQuery());
            var color = colors.FirstOrDefault(c => c.ColorId == id);
            if (color == null)
            {
                TempData["Error"] = "Color not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(color);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string colorName, string hexCode, bool isActive, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(colorName))
            {
                TempData["Error"] = "Color name is required.";
                return this.RedirectEncrypted(nameof(Edit), new { id });
            }

            try
            {
                var result = await _mediator.Send(new UpdateVehicleColorCommand
                {
                    ColorId = id,
                    ColorName = colorName.Trim(),
                    HexCode = string.IsNullOrWhiteSpace(hexCode) ? null : hexCode.Trim(),
                    IsActive = isActive,
                    ModifiedBy = userId.Value,
                    Remarks = remarks
                });

                TempData[result ? "Success" : "Error"] = result ? $"Color '{colorName}' updated!" : "Update failed.";
                return result ? RedirectToAction(nameof(Index)) : this.RedirectEncrypted(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return this.RedirectEncrypted(nameof(Edit), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var colors = await _mediator.Send(new GetVehicleColorsQuery());
            var color = colors.FirstOrDefault(c => c.ColorId == id);
            if (color == null)
            {
                TempData["Error"] = "Color not found.";
                return RedirectToAction(nameof(Index));
            }

            await _mediator.Send(new UpdateVehicleColorCommand
            {
                ColorId = id,
                ColorName = color.ColorName,
                HexCode = color.HexCode,
                IsActive = false,
                ModifiedBy = userId.Value,
                Remarks = "Deactivated via admin delete"
            });

            TempData["Success"] = $"Color '{color.ColorName}' deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

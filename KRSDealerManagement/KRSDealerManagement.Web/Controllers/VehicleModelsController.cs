using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)] // Admin only
    public class VehicleModelsController : Controller
    {
        private readonly IMediator _mediator;

        public VehicleModelsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: VehicleModels
        public async Task<IActionResult> Index(string searchTerm, bool? isActive, int? page)
        {
            var query = new GetVehicleModelsQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive
            };

            var models = await _mediator.Send(query);
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(models, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsActive = isActive;

            return View(pageItems);
        }

        public async Task<IActionResult> Export(string searchTerm, bool? isActive)
        {
            var models = (await _mediator.Send(new GetVehicleModelsQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive
            })).ToList();
            var headers = new[] { "Model", "Description", "Status", "Created" };
            var rows = models.Select(m => (IReadOnlyList<object?>)new List<object?>
            {
                m.ModelName, m.Description ?? "", m.IsActive ? "Active" : "Inactive", m.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"vehicle_models_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Models");
        }

        // GET: VehicleModels/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VehicleModels/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string modelName, string description)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                TempData["Error"] = "Model name is required.";
                return View();
            }

            var command = new CreateVehicleModelCommand
            {
                ModelName = modelName.Trim(),
                Description = description?.Trim() ?? "",
                CreatedBy = userId.Value
            };

            try
            {
                var modelId = await _mediator.Send(command);
                TempData["Success"] = $"Vehicle model '{modelName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating vehicle model: {ex.Message}";
                return View();
            }
        }

        // GET: VehicleModels/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var query = new GetVehicleModelByIdQuery { ModelId = id };
            var model = await _mediator.Send(query);

            if (model == null)
            {
                TempData["Error"] = "Vehicle model not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: VehicleModels/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string modelName, string description, bool isActive, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                TempData["Error"] = "Model name is required.";
                return this.RedirectEncrypted(nameof(Edit), new { id });
            }

            var command = new UpdateVehicleModelCommand
            {
                ModelId = id,
                ModelName = modelName.Trim(),
                Description = description?.Trim() ?? "",
                IsActive = isActive,
                ModifiedBy = userId.Value,
                Remarks = remarks?.Trim() ?? ""
            };

            try
            {
                var result = await _mediator.Send(command);

                if (result)
                {
                    TempData["Success"] = $"Vehicle model '{modelName}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Vehicle model not found or update failed.";
                    return this.RedirectEncrypted(nameof(Edit), new { id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating vehicle model: {ex.Message}";
                return this.RedirectEncrypted(nameof(Edit), new { id });
            }
        }

        // GET: VehicleModels/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var query = new GetVehicleModelByIdQuery { ModelId = id };
            var model = await _mediator.Send(query);

            if (model == null)
            {
                TempData["Error"] = "Vehicle model not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var model = await _mediator.Send(new GetVehicleModelByIdQuery { ModelId = id });
            if (model == null)
            {
                TempData["Error"] = "Vehicle model not found.";
                return RedirectToAction(nameof(Index));
            }

            await _mediator.Send(new UpdateVehicleModelCommand
            {
                ModelId = id,
                ModelName = model.ModelName,
                Description = model.Description ?? "",
                IsActive = false,
                ModifiedBy = userId.Value,
                Remarks = "Deactivated via admin delete"
            });

            TempData["Success"] = $"Vehicle model '{model.ModelName}' deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

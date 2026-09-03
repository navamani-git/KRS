using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 4)]
    public class VehicleMastersController : Controller
    {
        private readonly IMediator _mediator;

        public VehicleMastersController(IMediator mediator) => _mediator = mediator;

        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Index(string? searchTerm, bool? isAllocated, int? dealershipId, int? page, int? pageSize)
        {
            var effectiveDealershipId = ResolveDealershipFilter(dealershipId);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.DealerStock);
            var masters = GridScreenFilterHelper.ApplyDealerStock(
                await _mediator.Send(new GetVehicleMastersQuery
                {
                    DealershipId = effectiveDealershipId,
                    IsAllocated = isAllocated,
                    SearchTerm = searchTerm
                }),
                columnFilters).ToList();

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(masters, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsAllocated = isAllocated;
            ViewBag.SelectedDealershipId = dealershipId;
            ViewBag.ShowBranchColumn = true;
            ViewBag.ShowDealershipFilter = SessionHelper.IsSystemAdmin(HttpContext.Session);
            ViewBag.ShowDealershipColumn = true;
            if (SessionHelper.IsSystemAdmin(HttpContext.Session))
                ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            ViewBag.TransferDealerships = (await _mediator.Send(new GetDealershipsQuery { IsActive = true }))
                .OrderBy(d => d.DealershipName)
                .ToList();
            ViewBag.IsAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            return View(pageItems);
        }

        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Export(string? searchTerm, bool? isAllocated, int? dealershipId)
        {
            var effectiveDealershipId = ResolveDealershipFilter(dealershipId);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.DealerStock);
            var masters = GridScreenFilterHelper.ApplyDealerStock(
                await _mediator.Send(new GetVehicleMastersQuery
                {
                    DealershipId = effectiveDealershipId,
                    IsAllocated = isAllocated,
                    SearchTerm = searchTerm
                }),
                columnFilters).ToList();

            var headers = new[] { "Branch", "Chassis", "Model", "Color", "Motor", "Battery", "Charger", "Controller", "Converter", "Invoice No", "Ampere Invoice", "Received", "Status", "Allocated To", "Remarks" };
            var rows = masters.Select(m => (IReadOnlyList<object?>)new List<object?>
            {
                m.DealershipName, m.ChassisNumber, m.ModelName, m.ColorName, m.MotorNo, m.BatteryNo, m.ChargerNo, m.ControllerNo, m.ConverterNo,
                m.AmpereInvoiceNo, m.AmpereInvoiceDate, m.ReceivedDate, m.IsAllocated ? "Allocated" : "Available",
                m.AllocatedToSubdealerName ?? "", m.Remarks ?? ""
            });
            return ExcelExportHelper.ToFileResult(this, $"dealer_stock_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Dealer Stock");
        }

        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Create()
        {
            await SetupFormViewBagAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Create(
            int? dealershipId,
            string chassisNumber, int modelId, int colorId,
            string motorNo, string batteryNo, string chargerNo, string controllerNo, string converterNo,
            string ampereInvoiceNo, DateTime ampereInvoiceDate, DateTime receivedDate, string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var resolvedDealershipId = ResolveDealershipId(dealershipId);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            if (!resolvedDealershipId.HasValue)
            {
                TempData["Error"] = SessionHelper.IsSystemAdmin(HttpContext.Session)
                    ? "Please select a dealership."
                    : "Your account is not linked to a dealership.";
                await SetupFormViewBagAsync();
                return View();
            }

            if (modelId <= 0 || colorId <= 0)
            {
                TempData["Error"] = "Please select a valid model and color.";
                await SetupFormViewBagAsync();
                return View();
            }

            try
            {
                await _mediator.Send(new CreateVehicleMasterCommand
                {
                    DealershipId = resolvedDealershipId.Value,
                    ChassisNumber = chassisNumber,
                    ModelId = modelId,
                    ColorId = colorId,
                    MotorNo = motorNo,
                    BatteryNo = batteryNo,
                    ChargerNo = chargerNo,
                    ControllerNo = controllerNo,
                    ConverterNo = converterNo,
                    AmpereInvoiceNo = ampereInvoiceNo,
                    AmpereInvoiceDate = ampereInvoiceDate,
                    ReceivedDate = receivedDate,
                    Remarks = remarks,
                    CreatedBy = userId.Value
                });
                TempData["Success"] = "Vehicle added to dealer stock.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await SetupFormViewBagAsync();
                return View();
            }
        }

        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Edit(int id)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var master = (await _mediator.Send(new GetVehicleMastersQuery { DealershipId = scope }))
                .FirstOrDefault(m => m.VehicleMasterId == id);
            if (master == null)
            {
                TempData["Error"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }
            if (master.IsAllocated)
            {
                TempData["Error"] = "Allocated vehicles cannot be edited.";
                return RedirectToAction(nameof(Index));
            }
            await SetupFormViewBagAsync();
            return View(master);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Edit(
            int vehicleMasterId, int modelId, int colorId,
            string motorNo, string batteryNo, string chargerNo, string controllerNo, string converterNo,
            string ampereInvoiceNo, DateTime ampereInvoiceDate, DateTime receivedDate, string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var master = (await _mediator.Send(new GetVehicleMastersQuery { DealershipId = scope }))
                .FirstOrDefault(m => m.VehicleMasterId == vehicleMasterId);
            if (master == null)
            {
                TempData["Error"] = "Vehicle not found or outside your branch scope.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new UpdateVehicleMasterCommand
                {
                    VehicleMasterId = vehicleMasterId,
                    ModelId = modelId,
                    ColorId = colorId,
                    MotorNo = motorNo,
                    BatteryNo = batteryNo,
                    ChargerNo = chargerNo,
                    ControllerNo = controllerNo,
                    ConverterNo = converterNo,
                    AmpereInvoiceNo = ampereInvoiceNo,
                    AmpereInvoiceDate = ampereInvoiceDate,
                    ReceivedDate = receivedDate,
                    Remarks = remarks,
                    ModifiedBy = userId.Value
                });
                TempData["Success"] = "Vehicle master updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id = vehicleMasterId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Delete(int id, string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var master = (await _mediator.Send(new GetVehicleMastersQuery { DealershipId = scope }))
                .FirstOrDefault(m => m.VehicleMasterId == id);
            if (master == null)
            {
                TempData["Error"] = "Vehicle not found or outside your branch scope.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new DeleteVehicleMasterCommand
                {
                    VehicleMasterId = id,
                    DeletedBy = userId.Value,
                    Remarks = remarks
                });
                TempData["Success"] = "Vehicle removed from dealer stock.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.DealerStock)]
        public async Task<IActionResult> Transfer(int id, int targetDealershipId, string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var master = (await _mediator.Send(new GetVehicleMastersQuery { DealershipId = scope }))
                .FirstOrDefault(m => m.VehicleMasterId == id);
            if (master == null && scope.HasValue)
            {
                TempData["Error"] = "Vehicle not found in your branch.";
                return RedirectToAction(nameof(Index));
            }

            if (master == null)
            {
                master = (await _mediator.Send(new GetVehicleMastersQuery()))
                    .FirstOrDefault(m => m.VehicleMasterId == id);
            }

            if (master == null)
            {
                TempData["Error"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }

            if (scope.HasValue && master.DealershipId != scope.Value)
            {
                TempData["Error"] = "You can only transfer vehicles from your branch.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new TransferVehicleMasterCommand
                {
                    VehicleMasterId = id,
                    TargetDealershipId = targetDealershipId,
                    TransferredBy = userId.Value,
                    Remarks = remarks
                });
                TempData["Success"] = $"Vehicle {master.ChassisNumber} transferred successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Available(int modelId, int colorId, int? dealershipId)
        {
            var scope = dealershipId ?? SessionHelper.GetDealershipScope(HttpContext.Session);
            if (!scope.HasValue)
                return Json(Array.Empty<object>());

            var options = await _mediator.Send(new GetAvailableVehicleMastersQuery
            {
                DealershipId = scope.Value,
                ModelId = modelId,
                ColorId = colorId
            });
            return Json(options);
        }

        private async Task SetupFormViewBagAsync()
        {
            ViewBag.Models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            ViewBag.Colors = await _mediator.Send(new GetVehicleColorsQuery { IsActive = true });
            ViewBag.RequireDealershipSelection = SessionHelper.IsSystemAdmin(HttpContext.Session);
            if (SessionHelper.IsSystemAdmin(HttpContext.Session))
                ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);
        }

        private int? ResolveDealershipId(int? dealershipId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            if (scope.HasValue)
                return scope.Value;
            if (SessionHelper.IsSystemAdmin(HttpContext.Session) && dealershipId is > 0)
                return dealershipId;
            return null;
        }

        private int? ResolveDealershipFilter(int? dealershipId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            if (scope.HasValue)
                return scope.Value;
            return dealershipId is > 0 ? dealershipId : null;
        }
    }
}

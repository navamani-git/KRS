using MediatR;
using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    public class WarrantyOnlyVehiclesController : Controller
    {
        private readonly IMediator _mediator;

        public WarrantyOnlyVehiclesController(IMediator mediator) => _mediator = mediator;

        [AuthorizeMenu(StaffMenuAccess.WarrantyOnlyStock, StaffOnly = true)]
        public async Task<IActionResult> Index(string? searchTerm, int? page, int? pageSize)
        {
            var query = new GetVehicleMastersQuery
            {
                WarrantyOnly = true,
                SearchTerm = searchTerm
            };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, query);
            var masters = (await _mediator.Send(query)).OrderBy(m => m.ChassisNumber).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(masters, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CanEdit = SessionHelper.CanWriteMenu(HttpContext.Session, StaffMenuAccess.WarrantyOnlyStock);
            ViewBag.ShowBranchColumn = SessionHelper.IsSystemAdmin(HttpContext.Session);
            return View(pageItems);
        }

        [AuthorizeMenu(StaffMenuAccess.WarrantyOnlyStock, StaffOnly = true)]
        public async Task<IActionResult> Create()
        {
            if (!SessionHelper.CanWriteMenu(HttpContext.Session, StaffMenuAccess.WarrantyOnlyStock))
            {
                TempData["Error"] = "This screen is read-only for your role.";
                return RedirectToAction(nameof(Index));
            }

            await SetupFormViewBagAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyOnlyStock, StaffOnly = true)]
        public async Task<IActionResult> Create(
            int? dealershipId,
            int subDealerId,
            string chassisNumber,
            int modelId,
            int colorId,
            string? customerName,
            string? customerMobile,
            DateTime? saleDate,
            string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var resolvedDealershipId = await ResolveDealershipIdAsync(dealershipId);
            if (!resolvedDealershipId.HasValue)
            {
                TempData["Error"] = SessionHelper.IsSystemAdmin(HttpContext.Session)
                    ? "Please select a dealership."
                    : "Your account is not linked to a dealership.";
                await SetupFormViewBagAsync();
                return View();
            }

            if (modelId <= 0 || colorId <= 0 || string.IsNullOrWhiteSpace(chassisNumber) || subDealerId <= 0)
            {
                TempData["Error"] = "Chassis, model, color, and Own Showroom subdealer are required.";
                await SetupFormViewBagAsync(resolvedDealershipId);
                return View();
            }

            try
            {
                await _mediator.Send(new CreateWarrantyOnlyVehicleMasterCommand
                {
                    DealershipId = resolvedDealershipId.Value,
                    SubDealerId = subDealerId,
                    ChassisNumber = chassisNumber,
                    ModelId = modelId,
                    ColorId = colorId,
                    CustomerName = customerName,
                    CustomerMobile = customerMobile,
                    SaleDate = saleDate,
                    Remarks = remarks,
                    CreatedBy = userId.Value
                });
                TempData["Success"] = "Warranty-only vehicle added.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await SetupFormViewBagAsync(resolvedDealershipId);
                return View();
            }
        }

        [AuthorizeMenu(StaffMenuAccess.WarrantyOnlyStock, StaffOnly = true)]
        public async Task<IActionResult> Edit(int id)
        {
            if (!SessionHelper.CanWriteMenu(HttpContext.Session, StaffMenuAccess.WarrantyOnlyStock))
            {
                TempData["Error"] = "This screen is read-only for your role.";
                return RedirectToAction(nameof(Index));
            }

            var query = new GetWarrantyOnlyVehicleEditQuery { VehicleMasterId = id };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, query);
            var editModel = await _mediator.Send(query);
            if (editModel == null)
            {
                TempData["Error"] = "Record not found.";
                return RedirectToAction(nameof(Index));
            }

            await SetupFormViewBagAsync();
            return View(editModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyOnlyStock, StaffOnly = true)]
        public async Task<IActionResult> Edit(
            int vehicleMasterId,
            int modelId,
            int colorId,
            string? customerName,
            string? customerMobile,
            DateTime? saleDate,
            string? remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var query = new GetWarrantyOnlyVehicleEditQuery { VehicleMasterId = vehicleMasterId };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, query);
            if (await _mediator.Send(query) == null)
            {
                TempData["Error"] = "Vehicle not found or outside your scope.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new UpdateWarrantyOnlyVehicleMasterCommand
                {
                    VehicleMasterId = vehicleMasterId,
                    ModelId = modelId,
                    ColorId = colorId,
                    CustomerName = customerName,
                    CustomerMobile = customerMobile,
                    SaleDate = saleDate,
                    Remarks = remarks,
                    ModifiedBy = userId.Value
                });
                TempData["Success"] = "Warranty-only vehicle updated.";
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
        [AuthorizeMenu(StaffMenuAccess.WarrantyOnlyStock, StaffOnly = true)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var master = await FindMasterAsync(id);
            if (master == null)
            {
                TempData["Error"] = "Vehicle not found or outside your scope.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new DeleteWarrantyOnlyVehicleMasterCommand
                {
                    VehicleMasterId = id,
                    DeletedBy = userId.Value,
                    Remarks = "Deleted from warranty-only stock"
                });
                TempData["Success"] = "Warranty-only vehicle removed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<Application.DTOs.VehicleMasterDto?> FindMasterAsync(int id)
        {
            var query = new GetVehicleMastersQuery { WarrantyOnly = true };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, query);
            return (await _mediator.Send(query)).FirstOrDefault(m => m.VehicleMasterId == id);
        }

        private async Task<int?> ResolveDealershipIdAsync(int? dealershipId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            if (scope.HasValue)
                return scope.Value;

            if (SessionHelper.IsSystemAdmin(HttpContext.Session) && dealershipId is > 0)
                return dealershipId;

            var dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            return dealerships.OrderBy(d => d.DealershipId).FirstOrDefault()?.DealershipId;
        }

        private async Task SetupFormViewBagAsync(int? dealershipId = null)
        {
            ViewBag.Models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            ViewBag.Colors = await _mediator.Send(new GetVehicleColorsQuery { IsActive = true });
            ViewBag.RequireDealershipSelection = SessionHelper.IsSystemAdmin(HttpContext.Session);
            if (SessionHelper.IsSystemAdmin(HttpContext.Session))
                ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });

            var subQuery = new GetSubdealersQuery { IsActive = true, OwnShowroomOnly = true };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, subQuery);
            var ownShowrooms = (await _mediator.Send(subQuery)).ToList();
            ViewBag.OwnShowroomSubdealers = ownShowrooms;
            ViewBag.HasOwnShowroom = ownShowrooms.Count > 0;
            ViewBag.SelectedDealershipId = dealershipId ?? SessionHelper.GetDealershipScope(HttpContext.Session);
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);
        }
    }
}

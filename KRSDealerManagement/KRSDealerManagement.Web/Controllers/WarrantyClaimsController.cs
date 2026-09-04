using MediatR;
using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    public class WarrantyClaimsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;
        private readonly IWebHostEnvironment _env;

        public WarrantyClaimsController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            IStatusLookupService statuses,
            IWebHostEnvironment env)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _statuses = statuses;
            _env = env;
        }

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> Index(int? status, string? claimType, int? page, int? pageSize)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.WarrantyClaims);
            var claims = GridScreenFilterHelper.ApplyWarrantyClaims(
                await _mediator.Send(new GetWarrantyClaimsQuery { Status = status, DealershipId = scope, ClaimType = claimType }),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(claims, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedClaimType = claimType;
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Warranty);
            return View(pageItems);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.MyWarrantyClaims)]
        public async Task<IActionResult> MyClaims(int? status, int? page, int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId!.Value);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.MyWarrantyClaims);
            var claims = GridScreenFilterHelper.ApplyMyWarrantyClaims(
                await _mediator.Send(new GetWarrantyClaimsQuery
                {
                    Status = status,
                    AccountId = account?.AccountId,
                    SubdealerUserId = userId
                }),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(claims, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Warranty);
            return View(pageItems);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.WarrantyApply)]
        public async Task<IActionResult> Create()
        {
            await LoadFormLookupsAsync();
            return View("Edit", new WarrantyClaimFormModel());
        }

        [AuthorizeRole(2)]
        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, MenuKeys.WarrantyApply)]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId!.Value);
            var detail = await _mediator.Send(new GetWarrantyClaimDetailQuery
            {
                WarrantyClaimId = id,
                AccountId = account?.AccountId
            });
            if (detail == null) return NotFound();
            if (!WarrantyClaimStatus.IsSubdealerEditable(detail.Status))
            {
                TempData["Error"] = "This claim cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }
            await LoadFormLookupsAsync();
            return View(MapToForm(detail));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(600_000_000)]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.WarrantyApply)]
        public async Task<IActionResult> Save(WarrantyClaimFormModel model, string action)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
            if (account == null)
            {
                TempData["Error"] = "Subdealer account not found.";
                return RedirectToAction(nameof(MyClaims));
            }

            var org = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .FirstOrDefault(o => o.UserId == userId && o.IsActive);

            try
            {
                await ResolveModelColorNamesAsync(model);

                var attachmentPaths = await BuildAttachmentPathsAsync(model);
                if (model.WarrantyClaimId > 0)
                {
                    var existing = await _mediator.Send(new GetWarrantyClaimDetailQuery
                    {
                        WarrantyClaimId = model.WarrantyClaimId,
                        AccountId = account.AccountId
                    });
                    if (existing != null)
                    {
                        foreach (var att in existing.Attachments)
                        {
                            if (!attachmentPaths.ContainsKey(att.AttachmentType))
                                attachmentPaths[att.AttachmentType] = att.FilePath;
                        }
                    }
                }

                var submit = string.Equals(action, "submit", StringComparison.OrdinalIgnoreCase);
                var claimId = await _mediator.Send(new SaveWarrantyClaimCommand
                {
                    WarrantyClaimId = model.WarrantyClaimId > 0 ? model.WarrantyClaimId : null,
                    Submit = submit,
                    UserId = userId,
                    AccountId = account.AccountId,
                    SubdealerId = userId,
                    DealershipId = org?.DealershipId,
                    ClaimType = model.ClaimType,
                    SubdealerVehicleId = model.SubdealerVehicleId,
                    ChassisNo = model.ChassisNo,
                    CustomerName = model.CustomerName,
                    CustomerMobile = model.CustomerMobile,
                    ContactPerson = model.ContactPerson,
                    ContactMobile = model.ContactMobile,
                    ModelId = model.ModelId,
                    ModelName = model.ModelName,
                    ColorId = model.ColorId,
                    ColorName = model.ColorName,
                    CurrentKms = model.CurrentKms,
                    SaleDate = model.SaleDate,
                    ComplaintDate = model.ComplaintDate,
                    WarrantyPartId = model.WarrantyPartId,
                    PartCode = model.PartCode,
                    FailurePartSerialNumber = model.FailurePartSerialNumber,
                    CustomerComplaint = model.CustomerComplaint,
                    DealerObservation = model.DealerObservation,
                    Remarks = model.Remarks,
                    ServiceEntries = model.ServiceEntries?.Select((e, i) => new WarrantyServiceEntryInput
                    {
                        ServiceType = e.ServiceType,
                        ServiceDate = e.ServiceDate,
                        ServiceKms = e.ServiceKms,
                        SortOrder = i
                    }).ToList() ?? new List<WarrantyServiceEntryInput>(),
                    AttachmentPaths = attachmentPaths
                });

                TempData["Success"] = submit
                    ? $"Warranty claim #{claimId} submitted successfully."
                    : $"Draft saved (claim #{claimId}).";
                return RedirectToAction(nameof(MyClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await LoadFormLookupsAsync();
                return View("Edit", model);
            }
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Details(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var isStaff = SessionHelper.IsStaff(HttpContext.Session);
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            int? accountId = null;
            if (!isStaff)
            {
                var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
                accountId = account?.AccountId;
            }

            var detail = await _mediator.Send(new GetWarrantyClaimDetailQuery
            {
                WarrantyClaimId = id,
                AccountId = accountId,
                DealershipId = isStaff && !SessionHelper.IsSystemAdmin(HttpContext.Session) ? scope : null,
                IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
            });
            if (detail == null) return NotFound();

            ViewBag.IsStaff = isStaff;
            ViewBag.IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            ViewBag.CanStaffEdit = isStaff && (SessionHelper.IsSystemAdmin(HttpContext.Session)
                || (await _unitOfWork.Users.GetByIdAsync(userId))?.CanEditWarrantyClaims == true);
            return View(detail);
        }

        [AuthorizeRole(2)]
        public async Task<IActionResult> LookupChassis(string chassis)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var result = await _mediator.Send(new GetWarrantyChassisLookupQuery { SubdealerUserId = userId, ChassisNo = chassis });
            return Json(result);
        }

        [AuthorizeRole(1, 2, 4)]
        public IActionResult DownloadAttachment(int id, string path)
        {
            if (!AppFileStorageHelper.TryResolveAbsolute(_env, path, out var absolute))
                return NotFound();
            var contentType = WarrantyFileHelper.GetContentType(absolute);
            return PhysicalFile(absolute, contentType, Path.GetFileName(absolute));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> Approve(int id, string? notes) => await StaffAction(id, notes, new ApproveWarrantyClaimCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> Reject(int id, string notes) => await StaffAction(id, notes, new RejectWarrantyClaimCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> RequestInfo(int id, string notes) => await StaffAction(id, notes, new RequestWarrantyInfoCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> ApplyToAmpere(int id, string? notes) => await StaffAction(id, notes, new ApplyWarrantyToAmpereCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> MarkProductReceived(int id, string? notes) => await StaffAction(id, notes, new MarkWarrantyProductReceivedCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> MarkDefectiveSentToAmpere(int id, string? notes) => await StaffAction(id, notes, new MarkWarrantyDefectiveSentToAmpereCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        public async Task<IActionResult> MarkCollected(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
            if (account == null) return RedirectToAction(nameof(MyClaims));
            var ok = await _mediator.Send(new MarkWarrantyCollectedCommand
            {
                WarrantyClaimId = id,
                UserId = userId,
                AccountId = account.AccountId
            });
            TempData[ok ? "Success" : "Error"] = ok ? "Product collection recorded." : "Unable to update claim.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        public async Task<IActionResult> MarkDefectiveSubmitted(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
            if (account == null) return RedirectToAction(nameof(MyClaims));
            var ok = await _mediator.Send(new MarkWarrantyDefectiveSubmittedCommand
            {
                WarrantyClaimId = id,
                UserId = userId,
                AccountId = account.AccountId
            });
            TempData[ok ? "Success" : "Error"] = ok ? "Defective product submission recorded." : "Unable to update claim.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<IActionResult> StaffAction(int id, string? notes, WarrantyClaimActionCommand command)
        {
            command.WarrantyClaimId = id;
            command.UserId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            command.Notes = notes;
            command.IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var ok = await _mediator.Send(command);
            TempData[ok ? "Success" : "Error"] = ok ? "Claim updated." : "Unable to update claim. Check status and required notes.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task LoadFormLookupsAsync()
        {
            ViewBag.Parts = (await _unitOfWork.WarrantyParts.GetAllAsync())
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.PartName)
                .ToList();
            ViewBag.Models = (await _unitOfWork.VehicleModels.GetAllAsync())
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModelName)
                .ToList();
            ViewBag.Colors = (await _unitOfWork.VehicleColors.GetAllAsync())
                .Where(c => c.IsActive)
                .OrderBy(c => c.ColorName)
                .ToList();
            ViewBag.ServiceTypes = WarrantyServiceTypes.All;
            ViewBag.ClaimTypes = WarrantyClaimTypes.All;
            ViewBag.WarrantyAttachmentTypes = WarrantyAttachmentTypes.RequiredForWarranty;
            ViewBag.CampaignAttachmentTypes = WarrantyAttachmentTypes.RequiredForCampaign;
        }

        private static WarrantyClaimFormModel MapToForm(Application.DTOs.WarrantyClaimDetailDto detail) => new()
        {
            WarrantyClaimId = detail.WarrantyClaimId,
            ClaimType = detail.ClaimType,
            SubdealerVehicleId = detail.SubdealerVehicleId,
            ChassisNo = detail.ChassisNo,
            CustomerName = detail.CustomerName,
            CustomerMobile = detail.CustomerMobile,
            ContactPerson = detail.ContactPerson,
            ContactMobile = detail.ContactMobile,
            ModelId = detail.ModelId,
            ModelName = detail.ModelName,
            ColorId = detail.ColorId,
            ColorName = detail.ColorName,
            CurrentKms = detail.CurrentKms,
            SaleDate = detail.SaleDate,
            ComplaintDate = detail.ComplaintDate,
            WarrantyPartId = detail.WarrantyPartId,
            PartCode = detail.PartCode,
            FailurePartSerialNumber = detail.FailurePartSerialNumber,
            CustomerComplaint = detail.CustomerComplaint,
            DealerObservation = detail.DealerObservation,
            Remarks = detail.Remarks,
            ServiceEntries = detail.ServiceEntries.Select(e => new WarrantyServiceEntryFormModel
            {
                ServiceType = e.ServiceType,
                ServiceDate = e.ServiceDate,
                ServiceKms = e.ServiceKms
            }).ToList()
        };

        private async Task ResolveModelColorNamesAsync(WarrantyClaimFormModel model)
        {
            if (model.ModelId.HasValue)
            {
                var m = await _unitOfWork.VehicleModels.GetByIdAsync(model.ModelId.Value);
                if (m != null) model.ModelName = m.ModelName;
            }
            if (model.ColorId.HasValue)
            {
                var c = await _unitOfWork.VehicleColors.GetByIdAsync(model.ColorId.Value);
                if (c != null) model.ColorName = c.ColorName;
            }
        }

        private async Task<Dictionary<string, string>> BuildAttachmentPathsAsync(WarrantyClaimFormModel model)
        {
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Request.Form.Files)
            {
                if (!file.Name.StartsWith("attachment_", StringComparison.OrdinalIgnoreCase) || file.Length == 0)
                    continue;
                var type = file.Name["attachment_".Length..];
                paths[type] = await WarrantyFileHelper.SaveAsync(file, _env);
            }
            return paths;
        }
    }
}

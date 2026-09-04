using MediatR;
using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
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
        private readonly ILogger<WarrantyClaimsController> _logger;

        public WarrantyClaimsController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            IStatusLookupService statuses,
            IWebHostEnvironment env,
            ILogger<WarrantyClaimsController> logger)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _statuses = statuses;
            _env = env;
            _logger = logger;
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
        [RequestFormLimits(MultipartBodyLengthLimit = 600_000_000)]
        [AuthorizeRole(2)]
        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, MenuKeys.WarrantyApply)]
        public async Task<IActionResult> Save(WarrantyClaimFormModel model, string action)
        {
            model ??= new WarrantyClaimFormModel();
            EnsureServiceEntries(model);

            try
            {
                var userId = SessionHelper.GetUserId(HttpContext.Session);
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId.Value);
                if (account == null)
                {
                    TempData["Error"] = "Subdealer account not found.";
                    return RedirectToAction(nameof(MyClaims));
                }

                var org = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                    .FirstOrDefault(o => o.UserId == userId.Value && o.IsActive);

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
                    UserId = userId.Value,
                    AccountId = account.AccountId,
                    SubdealerId = userId.Value,
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
                    OtherPartName = model.OtherPartName,
                    PartCode = model.PartCode,
                    FailurePartSerialNumber = model.FailurePartSerialNumber,
                    CustomerComplaint = model.CustomerComplaint,
                    DealerObservation = model.DealerObservation,
                    Remarks = model.Remarks,
                    ServiceEntries = model.ServiceEntries.Select((e, i) => new WarrantyServiceEntryInput
                    {
                        ServiceType = e.ServiceType,
                        ServiceDate = e.ServiceDate,
                        ServiceKms = e.ServiceKms,
                        SortOrder = i
                    }).ToList(),
                    AttachmentPaths = attachmentPaths
                });

                TempData["Success"] = submit
                    ? $"Warranty claim #{claimId} submitted successfully."
                    : $"Draft saved (claim #{claimId}).";
                return submit
                    ? RedirectToAction(nameof(MyClaims))
                    : RedirectToAction(nameof(Edit), new { id = claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Warranty claim save failed for claim {ClaimId}", model.WarrantyClaimId);
                TempData["Error"] = ex.Message;
                try
                {
                    EnsureServiceEntries(model);
                    await LoadFormLookupsAsync();
                    return View("Edit", model);
                }
                catch (Exception viewEx)
                {
                    _logger.LogError(viewEx, "Failed to render warranty claim form after save error");
                    TempData["Error"] = ex.Message;
                    return RedirectToAction(model.WarrantyClaimId > 0 ? nameof(Edit) : nameof(Create),
                        model.WarrantyClaimId > 0 ? new { id = model.WarrantyClaimId } : null);
                }
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
        public async Task<IActionResult> ViewAttachment(int id, string path)
        {
            var access = await TryResolveClaimAttachmentAsync(id, path);
            if (access == null)
                return NotFound();

            var contentType = WarrantyFileHelper.GetContentType(access);
            return PhysicalFile(access, contentType);
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> DownloadAttachment(int id, string path)
        {
            var access = await TryResolveClaimAttachmentAsync(id, path);
            if (access == null)
                return NotFound();

            var contentType = WarrantyFileHelper.GetContentType(access);
            return PhysicalFile(access, contentType, Path.GetFileName(access));
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
        public async Task<IActionResult> UpdateSoNumber(int id, string soNumber)
        {
            if (string.IsNullOrWhiteSpace(soNumber))
            {
                TempData["Error"] = "SO Number is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return await StaffAction(id, null, new UpdateWarrantySoNumberCommand { SoNumber = soNumber.Trim() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> ApplyToAmpere(int id, string? soNumber, string? notes)
        {
            return await StaffAction(id, notes, new ApplyWarrantyToAmpereCommand { SoNumber = soNumber?.Trim() ?? "" });
        }

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

        private static void EnsureServiceEntries(WarrantyClaimFormModel model)
        {
            if (model.ServiceEntries is { Count: > 0 })
                return;

            model.ServiceEntries = new List<WarrantyServiceEntryFormModel> { new(), new() };
        }

        private async Task<IActionResult> StaffAction(int id, string? notes, WarrantyClaimActionCommand command)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            if (!isAdmin)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user?.CanEditWarrantyClaims != true)
                {
                    TempData["Error"] = "You do not have permission to update warranty claims.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            command.WarrantyClaimId = id;
            command.UserId = userId;
            command.Notes = notes;
            command.IsSystemAdmin = isAdmin;
            var ok = await _mediator.Send(command);
            TempData[ok ? "Success" : "Error"] = ok ? "Claim updated." : "Unable to update claim. Check status and required notes.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task LoadFormLookupsAsync()
        {
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);
            ViewBag.Parts = (await _unitOfWork.WarrantyParts.GetAllAsync())
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.PartName)
                .ToList();
            ViewBag.OthersPartId = (await _unitOfWork.WarrantyParts.GetAllAsync())
                .FirstOrDefault(p => p.IsActive && WarrantyPartHelper.IsOthersPart(p))
                ?.WarrantyPartId;
            ViewBag.Models = (await _unitOfWork.VehicleModels.GetAllAsync())
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModelName)
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
            OtherPartName = detail.OtherPartName,
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

        private async Task<string?> TryResolveClaimAttachmentAsync(int claimId, string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !AppFileStorageHelper.TryResolveAbsolute(_env, path, out var absolute))
                return null;

            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return null;

            var isStaff = SessionHelper.IsStaff(HttpContext.Session);
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            int? accountId = null;
            if (!isStaff)
            {
                var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId.Value);
                accountId = account?.AccountId;
            }

            var detail = await _mediator.Send(new GetWarrantyClaimDetailQuery
            {
                WarrantyClaimId = claimId,
                AccountId = accountId,
                DealershipId = isStaff && !SessionHelper.IsSystemAdmin(HttpContext.Session) ? scope : null,
                IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
            });
            if (detail == null)
                return null;

            return detail.Attachments.Any(a =>
                string.Equals(a.FilePath, path, StringComparison.OrdinalIgnoreCase))
                ? absolute
                : null;
        }
    }
}

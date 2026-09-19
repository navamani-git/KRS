using MediatR;
using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.DTOs;
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

        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> Index(int? status, string? claimType, int? subdealerUserId, int? page, int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.WarrantyClaims);
            var claimsQuery = new GetWarrantyClaimsQuery
            {
                Status = status,
                ClaimType = claimType,
                ExcludeDraft = true,
                ExcludeComplete = true,
                SubdealerUserId = subdealerUserId
            };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, claimsQuery);
            var claims = GridScreenFilterHelper.ApplyWarrantyClaims(
                await _mediator.Send(claimsQuery),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(claims, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedClaimType = claimType;
            ViewBag.SelectedSubdealerUserId = subdealerUserId;

            var subdealersQuery = new GetSubdealersQuery { IsActive = true };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, subdealersQuery);
            ViewBag.Subdealers = (await _mediator.Send(subdealersQuery))
                .Where(s => s.UserId > 0)
                .OrderBy(s => s.GetFullName())
                .ToList();

            ViewBag.Statuses = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Warranty))
                .Where(s => s.StatusValue != WarrantyClaimStatus.Draft && s.StatusValue != WarrantyClaimStatus.Complete);
            return View(pageItems);
        }

        [AuthorizeMenu(StaffMenuAccess.WarrantyCompleted, StaffOnly = true)]
        public async Task<IActionResult> Completed(int? subdealerUserId, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var claimsQuery = new GetWarrantyClaimsQuery
            {
                OnlyComplete = true,
                CompletedFromDate = fromDate,
                CompletedToDate = toDate,
                SubdealerUserId = subdealerUserId
            };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, claimsQuery);
            var claims = (await _mediator.Send(claimsQuery)).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(claims, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            var subdealersQuery = new GetSubdealersQuery { IsActive = true };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, subdealersQuery);
            ViewBag.Subdealers = (await _mediator.Send(subdealersQuery))
                .Where(s => s.UserId > 0)
                .OrderBy(s => s.GetFullName())
                .ToList();
            ViewBag.SelectedSubdealerUserId = subdealerUserId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.IsStaff = true;
            return View("Completed", pageItems);
        }

        [AuthorizeMenu(MenuKeys.MyCompletedWarrantyClaims, SubdealerOnly = true)]
        public async Task<IActionResult> MyCompleted(DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId!.Value);
            var claims = (await _mediator.Send(new GetWarrantyClaimsQuery
            {
                OnlyComplete = true,
                CompletedFromDate = fromDate,
                CompletedToDate = toDate,
                AccountId = account?.AccountId,
                SubdealerUserId = userId
            })).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(claims, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.IsStaff = false;
            return View("Completed", pageItems);
        }

        [AuthorizeMenu(MenuKeys.MyWarrantyClaims, SubdealerOnly = true)]
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
                    SubdealerUserId = userId,
                    ExcludeComplete = true
                }),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(claims, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SelectedStatus = status;
            ViewBag.Statuses = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Warranty))
                .Where(s => s.StatusValue != WarrantyClaimStatus.Complete);
            return View(pageItems);
        }

        [AuthorizeMenu(StaffMenuAccess.WarrantyApply, StaffOnly = true)]
        public async Task<IActionResult> StaffCreate()
        {
            if (!SessionHelper.CanWriteMenu(HttpContext.Session, StaffMenuAccess.WarrantyApply))
            {
                TempData["Error"] = "This screen is read-only for your role.";
                return RedirectToAction(nameof(Index));
            }

            await LoadFormLookupsAsync(staffApply: true);
            return View("Edit", new WarrantyClaimFormModel { IsStaffApply = true });
        }

        [AuthorizeMenu(MenuKeys.WarrantyApply, SubdealerOnly = true)]
        public async Task<IActionResult> Create()
        {
            if (!SessionHelper.CanWriteMenu(HttpContext.Session, MenuKeys.WarrantyApply))
            {
                TempData["Error"] = "This screen is read-only for your role.";
                return RedirectToAction(nameof(MyClaims));
            }

            await LoadFormLookupsAsync();
            return View("Edit", new WarrantyClaimFormModel());
        }

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
            if (!SessionHelper.CanWriteMenu(HttpContext.Session, MenuKeys.MyWarrantyClaims)
                && !SessionHelper.CanWriteMenu(HttpContext.Session, MenuKeys.WarrantyApply))
            {
                TempData["Error"] = "This screen is read-only for your role.";
                return RedirectToAction(nameof(Details), new { id });
            }
            await LoadFormLookupsAsync();
            var form = MapToForm(detail);
            EnsureServiceEntries(form);
            if (!string.IsNullOrWhiteSpace(form.ChassisNo))
            {
                var master = await _unitOfWork.VehicleMasters.GetByChassisAsync(form.ChassisNo.Trim());
                form.VehicleMasterId = master?.VehicleMasterId;
            }
            ViewBag.ExistingAttachments = detail.Attachments;
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(600_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 600_000_000)]
        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, MenuKeys.WarrantyApply, StaffMenuAccess.WarrantyApply)]
        public async Task<IActionResult> Save(WarrantyClaimFormModel model, string? saveMode)
        {
            model ??= new WarrantyClaimFormModel();
            EnsureServiceEntries(model);
            if (string.IsNullOrWhiteSpace(saveMode))
                saveMode = Request.Form["saveMode"].FirstOrDefault();

            var isStaff = SessionHelper.IsStaff(HttpContext.Session);
            var isStaffApply = model.IsStaffApply && isStaff;
            if (isStaffApply)
            {
                if (!SessionHelper.CanWriteMenu(HttpContext.Session, StaffMenuAccess.WarrantyApply))
                {
                    TempData["Error"] = "This screen is read-only for your role.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (isStaff)
            {
                TempData["Error"] = "Invalid staff warranty apply request.";
                return RedirectToAction(nameof(Index));
            }
            else if (!SessionHelper.CanWriteMenu(HttpContext.Session, MenuKeys.MyWarrantyClaims)
                && !SessionHelper.CanWriteMenu(HttpContext.Session, MenuKeys.WarrantyApply))
            {
                TempData["Error"] = "This screen is read-only for your role.";
                return RedirectToAction(nameof(MyClaims));
            }

            try
            {
                var userId = SessionHelper.GetUserId(HttpContext.Session);
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                int targetSubdealerId;
                int accountId;
                int? dealershipId;

                if (isStaffApply)
                {
                    if (!model.TargetSubdealerUserId.HasValue || model.TargetSubdealerUserId.Value <= 0)
                        throw new InvalidOperationException("Please select a subdealer.");

                    targetSubdealerId = model.TargetSubdealerUserId.Value;
                    var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, targetSubdealerId)
                        ?? throw new InvalidOperationException("Subdealer account not found.");
                    accountId = account.AccountId;

                    var org = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                        .FirstOrDefault(o => o.UserId == targetSubdealerId && o.IsActive);
                    dealershipId = org?.DealershipId;
                }
                else
                {
                    var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId.Value);
                    if (account == null)
                    {
                        TempData["Error"] = "Subdealer account not found.";
                        return RedirectToAction(nameof(MyClaims));
                    }

                    targetSubdealerId = userId.Value;
                    accountId = account.AccountId;
                    var org = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                        .FirstOrDefault(o => o.UserId == userId.Value && o.IsActive);
                    dealershipId = org?.DealershipId;
                }

                await ResolveModelColorNamesAsync(model);

                var attachmentPaths = await BuildAttachmentPathsAsync(model);
                if (model.WarrantyClaimId > 0)
                {
                    var existing = await _mediator.Send(new GetWarrantyClaimDetailQuery
                    {
                        WarrantyClaimId = model.WarrantyClaimId,
                        AccountId = isStaffApply ? null : accountId
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

                var submit = string.Equals(saveMode, "submit", StringComparison.OrdinalIgnoreCase);
                var claimId = await _mediator.Send(new SaveWarrantyClaimCommand
                {
                    WarrantyClaimId = model.WarrantyClaimId > 0 ? model.WarrantyClaimId : null,
                    Submit = submit,
                    UserId = userId.Value,
                    AccountId = accountId,
                    SubdealerId = targetSubdealerId,
                    DealershipId = dealershipId,
                    ClaimType = model.ClaimType,
                    VehicleMasterId = model.VehicleMasterId,
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
                if (submit)
                    return isStaffApply
                        ? RedirectToAction(nameof(Index))
                        : RedirectToAction(nameof(MyClaims));
                return RedirectToAction(nameof(Edit), new { id = claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Warranty claim save failed for claim {ClaimId}", model.WarrantyClaimId);
                TempData["Error"] = ex.Message;
                try
                {
                    EnsureServiceEntries(model);
                    await LoadFormLookupsAsync(model.IsStaffApply);
                    return View("Edit", model);
                }
                catch (Exception viewEx)
                {
                    _logger.LogError(viewEx, "Failed to render warranty claim form after save error");
                    TempData["Error"] = ex.Message;
                    if (model.IsStaffApply)
                        return RedirectToAction(nameof(StaffCreate));
                    return RedirectToAction(model.WarrantyClaimId > 0 ? nameof(Edit) : nameof(Create),
                        model.WarrantyClaimId > 0 ? new { id = model.WarrantyClaimId } : null);
                }
            }
        }

        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, MenuKeys.MyCompletedWarrantyClaims, StaffMenuAccess.WarrantyClaims, StaffMenuAccess.WarrantyCompleted)]
        public async Task<IActionResult> Details(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var isStaff = SessionHelper.IsStaff(HttpContext.Session);
            int? accountId = null;
            if (!isStaff)
            {
                var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
                accountId = account?.AccountId;
            }

            var detailQuery = new GetWarrantyClaimDetailQuery
            {
                WarrantyClaimId = id,
                AccountId = accountId,
                IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
            };
            if (isStaff && !SessionHelper.IsSystemAdmin(HttpContext.Session))
                DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, detailQuery);

            var detail = await _mediator.Send(detailQuery);
            if (detail == null) return NotFound();

            ViewBag.IsStaff = isStaff;
            ViewBag.IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            ViewBag.CanStaffEdit = isStaff
                && SessionHelper.CanWriteMenu(HttpContext.Session, StaffMenuAccess.WarrantyClaims);
            ViewBag.CanStaffEditWorkflow = isStaff
                && (ViewBag.CanStaffEdit as bool? == true)
                && WarrantyClaimStatus.CanStaffEditPostAmpereWorkflow(detail.Status, detail.DefectiveSentToAmpereCompleted);
            return View(detail);
        }

        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> History(int id)
        {
            var detailQuery = new GetWarrantyClaimDetailQuery
            {
                WarrantyClaimId = id,
                IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
            };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, detailQuery);
            var detail = await _mediator.Send(detailQuery);
            if (detail == null) return NotFound();
            return PartialView("_ClaimHistory", detail);
        }

        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, MenuKeys.WarrantyApply, StaffMenuAccess.WarrantyApply, StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> LookupChassis(string chassis, int? subdealerUserId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var isStaff = SessionHelper.IsStaff(HttpContext.Session);
            var lookupUserId = isStaff && subdealerUserId.HasValue ? subdealerUserId : userId;
            var result = await _mediator.Send(new GetWarrantyChassisLookupQuery
            {
                ChassisNo = chassis,
                SubdealerUserId = lookupUserId
            });
            return Json(result);
        }

        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, MenuKeys.WarrantyApply, StaffMenuAccess.WarrantyApply, StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> SearchChassis(string? term, int take = 50)
        {
            var options = await _mediator.Send(new SearchWarrantyChassisQuery { Term = term, Take = take });
            return Json(options);
        }

        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, StaffMenuAccess.WarrantyClaims)]
        public async Task<IActionResult> ViewAttachment(int id, string path)
        {
            var access = await TryResolveClaimAttachmentAsync(id, path);
            if (access == null)
                return NotFound();

            var contentType = WarrantyFileHelper.GetContentType(access);
            return PhysicalFile(access, contentType);
        }

        [AuthorizeMenuAny(MenuKeys.MyWarrantyClaims, StaffMenuAccess.WarrantyClaims)]
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
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> Accept(int id, string? notes) => await StaffAction(id, notes, new AcceptWarrantyClaimCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> Reject(int id, string notes) => await StaffAction(id, notes, new RejectWarrantyClaimCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> RequestInfo(int id, string notes) => await StaffAction(id, notes, new RequestWarrantyInfoCommand());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
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
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> ApplyToAmpere(int id, string? notes, string? actionDate)
            => await StaffAction(id, notes, new ApplyWarrantyToAmpereCommand { ActionDate = ParseActionDate(actionDate) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> MarkAmpereApproved(int id, string? notes, string? actionDate)
            => await StaffAction(id, notes, new MarkWarrantyAmpereApprovedCommand { ActionDate = ParseActionDate(actionDate) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> SaveResolutionPart(int id, string dealerResolutionType, string dealerClosedPartNumber, string? notes)
            => await StaffAction(id, notes, new SaveWarrantyResolutionPartCommand
            {
                DealerResolutionType = dealerResolutionType?.Trim() ?? "",
                DealerClosedPartNumber = dealerClosedPartNumber?.Trim() ?? ""
            });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> SaveDealerInvoiceClosed(int id, string dealerClosedInvoiceNumber, string? actionDate, string? notes)
            => await StaffAction(id, notes, new SaveWarrantyDealerInvoiceClosedCommand
            {
                DealerClosedInvoiceNumber = dealerClosedInvoiceNumber?.Trim() ?? "",
                ActionDate = ParseActionDate(actionDate)
            });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> MarkReplacementPartReceived(int id, string? notes, string? actionDate)
            => await StaffAction(id, notes, new MarkWarrantyReplacementPartReceivedCommand { ActionDate = ParseActionDate(actionDate) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> MarkDefectiveSentToAmpere(int id, string? notes, string? actionDate)
            => await StaffAction(id, notes, new MarkWarrantyDefectiveSentToAmpereCommand { ActionDate = ParseActionDate(actionDate) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> StaffMarkSubdealerPartReceived(int id, string receivedByName, string? actionDate)
        {
            var claim = await _unitOfWork.WarrantyClaims.GetByIdAsync(id);
            if (claim == null) return RedirectToAction(nameof(Index));
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var ok = await _mediator.Send(new MarkWarrantySubdealerPartReceivedCommand
            {
                WarrantyClaimId = id,
                UserId = userId,
                AccountId = claim.AccountId,
                ReceivedByName = receivedByName?.Trim() ?? "",
                ActionDate = ParseActionDate(actionDate),
                OnBehalfOfSubdealerByStaff = true
            });
            TempData[ok ? "Success" : "Error"] = ok ? "Part receipt recorded on behalf of subdealer." : "Unable to update claim.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(StaffMenuAccess.WarrantyClaims, StaffOnly = true)]
        public async Task<IActionResult> StaffMarkDefectiveHandover(int id, string handoverByName, string? actionDate)
        {
            var claim = await _unitOfWork.WarrantyClaims.GetByIdAsync(id);
            if (claim == null) return RedirectToAction(nameof(Index));
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var ok = await _mediator.Send(new MarkWarrantyDefectiveHandoverCommand
            {
                WarrantyClaimId = id,
                UserId = userId,
                AccountId = claim.AccountId,
                HandoverByName = handoverByName?.Trim() ?? "",
                ActionDate = ParseActionDate(actionDate),
                OnBehalfOfSubdealerByStaff = true
            });
            TempData[ok ? "Success" : "Error"] = ok ? "Defective handover recorded on behalf of subdealer." : "Unable to update claim.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(MenuKeys.MyWarrantyClaims, SubdealerOnly = true)]
        public async Task<IActionResult> MarkSubdealerPartReceived(int id, string receivedByName, string? actionDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
            if (account == null) return RedirectToAction(nameof(MyClaims));
            var ok = await _mediator.Send(new MarkWarrantySubdealerPartReceivedCommand
            {
                WarrantyClaimId = id,
                UserId = userId,
                AccountId = account.AccountId,
                ReceivedByName = receivedByName?.Trim() ?? "",
                ActionDate = ParseActionDate(actionDate)
            });
            TempData[ok ? "Success" : "Error"] = ok ? "Part receipt recorded." : "Unable to update claim.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeMenu(MenuKeys.MyWarrantyClaims, SubdealerOnly = true)]
        public async Task<IActionResult> MarkDefectiveHandover(int id, string handoverByName, string? actionDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId);
            if (account == null) return RedirectToAction(nameof(MyClaims));
            var ok = await _mediator.Send(new MarkWarrantyDefectiveHandoverCommand
            {
                WarrantyClaimId = id,
                UserId = userId,
                AccountId = account.AccountId,
                HandoverByName = handoverByName?.Trim() ?? "",
                ActionDate = ParseActionDate(actionDate)
            });
            TempData[ok ? "Success" : "Error"] = ok ? "Defective handover recorded." : "Unable to update claim.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private static void EnsureServiceEntries(WarrantyClaimFormModel model)
        {
            model.ServiceEntries ??= new List<WarrantyServiceEntryFormModel>();
            while (model.ServiceEntries.Count < 5)
                model.ServiceEntries.Add(new());
        }

        private static DateTime? ParseActionDate(string? value)
            => DateTime.TryParse(value, out var d) ? d : null;

        private Task<IActionResult> StaffAction(int id, string? notes, WarrantyClaimActionCommand command)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
            command.WarrantyClaimId = id;
            command.UserId = userId;
            command.Notes = notes;
            command.IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            return ExecuteStaffActionAsync(command, id);
        }

        private async Task<IActionResult> ExecuteStaffActionAsync(WarrantyClaimActionCommand command, int id)
        {
            var ok = await _mediator.Send(command);
            TempData[ok ? "Success" : "Error"] = ok ? "Claim updated." : "Unable to update claim. Check status and required notes.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task LoadFormLookupsAsync(bool staffApply = false)
        {
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);
            ViewBag.IsStaffApply = staffApply;
            if (staffApply)
            {
                var subdealersQuery = new GetSubdealersQuery { IsActive = true };
                DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, subdealersQuery);
                ViewBag.Subdealers = (await _mediator.Send(subdealersQuery))
                    .Where(s => s.UserId > 0)
                    .OrderBy(s => s.GetFullName())
                    .ToList();
            }
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
            ServiceEntries = PadServiceEntries(detail.ServiceEntries)
        };

        private static List<WarrantyServiceEntryFormModel> PadServiceEntries(IEnumerable<WarrantyClaimServiceEntryDto> entries)
        {
            var list = entries.Select(e => new WarrantyServiceEntryFormModel
            {
                ServiceType = e.ServiceType,
                ServiceDate = e.ServiceDate,
                ServiceKms = e.ServiceKms
            }).ToList();
            while (list.Count < 5)
                list.Add(new WarrantyServiceEntryFormModel());
            return list;
        }

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
                paths[type] = await WarrantyFileHelper.SaveAsync(
                    file, _env, WarrantyAttachmentTypes.GetDisplayName(type));
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
            int? accountId = null;
            if (!isStaff)
            {
                var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, userId.Value);
                accountId = account?.AccountId;
            }

            var detailQuery = new GetWarrantyClaimDetailQuery
            {
                WarrantyClaimId = claimId,
                AccountId = accountId,
                IsSystemAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session)
            };
            if (isStaff && !SessionHelper.IsSystemAdmin(HttpContext.Session))
                DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, detailQuery);

            var detail = await _mediator.Send(detailQuery);
            if (detail == null)
                return null;

            return detail.Attachments.Any(a =>
                string.Equals(a.FilePath, path, StringComparison.OrdinalIgnoreCase))
                ? absolute
                : null;
        }
    }
}

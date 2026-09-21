using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class SaveWarrantyClaimCommandHandler : IRequestHandler<SaveWarrantyClaimCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public SaveWarrantyClaimCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(SaveWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            WarrantyClaim claim;
            var isNew = !request.WarrantyClaimId.HasValue || request.WarrantyClaimId.Value <= 0;
            int? fromStatus = null;

            if (isNew)
            {
                claim = new WarrantyClaim
                {
                    ClaimNumber = await WarrantyClaimWorkflowHelper.GenerateClaimNumberAsync(_unitOfWork),
                    Status = WarrantyClaimStatus.Draft,
                    CreatedByUserId = request.UserId,
                    CreatedDate = DateTime.UtcNow
                };
            }
            else
            {
                claim = await _unitOfWork.WarrantyClaims.GetByIdAsync(request.WarrantyClaimId!.Value)
                    ?? throw new InvalidOperationException("Claim not found.");
                if (claim.AccountId != request.AccountId)
                    throw new InvalidOperationException("Access denied.");
                if (!WarrantyClaimStatus.IsSubdealerEditable(claim.Status))
                    throw new InvalidOperationException("This claim cannot be edited in its current status.");
                fromStatus = claim.Status;
            }

            MapClaim(claim, request);

            await ApplyChassisFromMasterAsync(claim, request);

            if (request.Submit)
                ValidateForSubmit(request, claim);

            await ApplyPartSelectionAsync(claim, request);

            claim.ModifiedByUserId = request.UserId;
            claim.ModifiedDate = DateTime.UtcNow;

            if (request.Submit)
            {
                WarrantyClaimWorkflowHelper.ValidateRequiredAttachments(claim.ClaimType, request.AttachmentPaths);
                claim.Status = WarrantyClaimStatus.Submitted;
                claim.SubmittedDate = DateTime.UtcNow;
                claim.SubmittedByUserId = request.UserId;
            }

            int claimId;
            if (isNew)
            {
                claimId = await _unitOfWork.WarrantyClaims.AddAsync(claim);
                await WarrantyClaimWorkflowHelper.RecordHistoryAsync(
                    _unitOfWork, claimId, null, claim.Status, request.UserId,
                    request.Submit ? "Claim submitted" : "Draft saved");
            }
            else
            {
                claimId = claim.WarrantyClaimId;
                await _unitOfWork.WarrantyClaims.UpdateAsync(claim);
                if (request.Submit)
                    await WarrantyClaimWorkflowHelper.RecordHistoryAsync(
                        _unitOfWork, claimId, fromStatus, claim.Status, request.UserId,
                        fromStatus == WarrantyClaimStatus.MoreInfoRequested ? "Resubmitted after more info" : "Claim submitted");
            }

            await ReplaceServiceEntriesAsync(claimId, request.ServiceEntries);
            await ReplaceAttachmentsAsync(claimId, request.AttachmentPaths, request.UserId);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync("WarrantyClaim", claimId, request.Submit ? "Submit" : "Save", request.UserId, "Subdealer", claim.Status.ToString());
            return claimId;
        }

        private static void MapClaim(WarrantyClaim claim, SaveWarrantyClaimCommand request)
        {
            claim.ClaimType = string.IsNullOrWhiteSpace(request.ClaimType)
                ? WarrantyClaimTypes.Warranty
                : request.ClaimType.Trim().ToUpperInvariant();
            claim.AccountId = request.AccountId;
            claim.SubdealerId = request.SubdealerId;
            claim.DealershipId = request.DealershipId;
            claim.SubdealerVehicleId = request.SubdealerVehicleId;
            claim.ChassisNo = string.IsNullOrWhiteSpace(request.ChassisNo)
                ? ""
                : request.ChassisNo.Trim().ToUpperInvariant();
            claim.CustomerName = TrimUpper(request.CustomerName);
            claim.CustomerMobile = Trim(request.CustomerMobile);
            claim.ContactPerson = Trim(request.ContactPerson);
            claim.ContactMobile = Trim(request.ContactMobile);
            claim.ModelId = request.ModelId;
            claim.ModelName = Trim(request.ModelName);
            claim.ColorId = request.ColorId;
            claim.ColorName = Trim(request.ColorName);
            claim.CurrentKms = request.CurrentKms;
            claim.SaleDate = request.SaleDate?.Date;
            claim.ComplaintDate = request.ComplaintDate?.Date;
            claim.PartCode = TrimUpper(request.PartCode);
            claim.FailurePartSerialNumber = TrimUpper(request.FailurePartSerialNumber);
            claim.CustomerComplaint = Trim(request.CustomerComplaint);
            claim.DealerObservation = Trim(request.DealerObservation);
            claim.Remarks = Trim(request.Remarks);
        }

        private async Task ApplyPartSelectionAsync(WarrantyClaim claim, SaveWarrantyClaimCommand request)
        {
            if (!request.WarrantyPartId.HasValue)
            {
                claim.WarrantyPartId = null;
                claim.OtherPartName = null;
                return;
            }

            var part = await _unitOfWork.WarrantyParts.GetByIdAsync(request.WarrantyPartId.Value);
            if (part == null || !part.IsActive)
                throw new InvalidOperationException("Invalid part selected.");

            claim.WarrantyPartId = request.WarrantyPartId;
            if (WarrantyPartHelper.IsOthersPart(part))
            {
                if (string.IsNullOrWhiteSpace(request.OtherPartName))
                {
                    if (request.Submit)
                        throw new InvalidOperationException("Please specify the part name when Others is selected.");
                    claim.OtherPartName = null;
                }
                else
                {
                    claim.OtherPartName = request.OtherPartName.Trim().ToUpperInvariant();
                }
            }
            else
            {
                claim.OtherPartName = null;
            }
        }

        private async Task ApplyChassisFromMasterAsync(WarrantyClaim claim, SaveWarrantyClaimCommand request)
        {
            var chassis = claim.ChassisNo.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(chassis))
                return;

            VehicleMaster? master = null;
            if (request.VehicleMasterId is > 0)
            {
                master = await _unitOfWork.VehicleMasters.GetByIdAsync(request.VehicleMasterId.Value);
                if (master != null
                    && !master.ChassisNumber.Equals(chassis, StringComparison.OrdinalIgnoreCase))
                    master = null;
            }

            master ??= await _unitOfWork.VehicleMasters.GetByChassisAsync(chassis);
            if (master == null)
            {
                if (request.Submit)
                    throw new InvalidOperationException("This chassis is not registered. Please contact your dealer admin.");
                return;
            }

            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            models.TryGetValue(master.ModelId, out var model);
            colors.TryGetValue(master.ColorId, out var color);

            claim.ChassisNo = master.ChassisNumber;
            claim.ModelId = master.ModelId;
            claim.ModelName = model?.ModelName;
            claim.ColorId = master.ColorId;
            claim.ColorName = color?.ColorName;

            var vehicle = VehicleLifecycleHelper.GetActiveRowForMaster(
                await _unitOfWork.Vehicles.GetAllAsync(),
                master.VehicleMasterId);
            if (vehicle != null)
                claim.SubdealerVehicleId = vehicle.VehicleId;

            await ModelColorValidation.EnsureMappedAsync(_unitOfWork, master.ModelId, master.ColorId);
        }

        private static void ValidateForSubmit(SaveWarrantyClaimCommand request, WarrantyClaim claim)
        {
            if (string.IsNullOrWhiteSpace(claim.ChassisNo))
                throw new InvalidOperationException("Chassis number is required.");
            if (!claim.ModelId.HasValue || !claim.ColorId.HasValue)
                throw new InvalidOperationException("Model and color are required for the selected chassis.");
            if (!request.CurrentKms.HasValue)
                throw new InvalidOperationException("Current KMs is required.");
            if (string.IsNullOrWhiteSpace(request.ContactPerson))
                throw new InvalidOperationException("Contact person is required.");
            if (string.IsNullOrWhiteSpace(request.ContactMobile))
                throw new InvalidOperationException("Contact mobile is required.");
            if (string.IsNullOrWhiteSpace(request.CustomerComplaint))
                throw new InvalidOperationException("Customer complaint is required.");
            if (!request.WarrantyPartId.HasValue)
                throw new InvalidOperationException("Part name is required.");
        }

        private async Task ReplaceServiceEntriesAsync(int claimId, List<WarrantyServiceEntryInput> entries)
        {
            var existing = (await _unitOfWork.WarrantyClaimServiceEntries.GetAllAsync())
                .Where(e => e.WarrantyClaimId == claimId).ToList();
            foreach (var e in existing)
                await _unitOfWork.WarrantyClaimServiceEntries.DeleteAsync(e.ServiceEntryId);

            var order = 0;
            foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e.ServiceType)))
            {
                await _unitOfWork.WarrantyClaimServiceEntries.AddAsync(new WarrantyClaimServiceEntry
                {
                    WarrantyClaimId = claimId,
                    ServiceType = entry.ServiceType.Trim().ToUpperInvariant(),
                    ServiceDate = entry.ServiceDate?.Date,
                    ServiceKms = entry.ServiceKms,
                    SortOrder = order++
                });
            }
        }

        private async Task ReplaceAttachmentsAsync(int claimId, Dictionary<string, string> paths, int userId)
        {
            foreach (var (type, path) in paths.Where(p => !string.IsNullOrWhiteSpace(p.Value)))
            {
                var existing = (await _unitOfWork.WarrantyClaimAttachments.GetAllAsync())
                    .Where(a => a.WarrantyClaimId == claimId && a.AttachmentType == type && a.IsActive)
                    .ToList();
                foreach (var old in existing)
                {
                    old.IsActive = false;
                    await _unitOfWork.WarrantyClaimAttachments.UpdateAsync(old);
                }

                await _unitOfWork.WarrantyClaimAttachments.AddAsync(new WarrantyClaimAttachment
                {
                    WarrantyClaimId = claimId,
                    AttachmentType = type,
                    FilePath = path,
                    UploadedByUserId = userId,
                    UploadedDate = DateTime.UtcNow,
                    IsActive = true
                });
            }
        }

        private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static string? TrimUpper(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    public abstract class WarrantyClaimTransitionHandler<T> : IRequestHandler<T, bool> where T : WarrantyClaimActionCommand
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly IAuditService AuditService;

        protected WarrantyClaimTransitionHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            UnitOfWork = unitOfWork;
            AuditService = auditService;
        }

        public async Task<bool> Handle(T request, CancellationToken cancellationToken)
        {
            var claim = await UnitOfWork.WarrantyClaims.GetByIdAsync(request.WarrantyClaimId);
            if (claim == null) return false;
            if (!CanTransition(claim, request)) return false;

            var from = claim.Status;
            ApplyTransition(claim, request);
            claim.ModifiedByUserId = request.UserId;
            claim.ModifiedDate = DateTime.UtcNow;
            await UnitOfWork.WarrantyClaims.UpdateAsync(claim);
            await WarrantyClaimWorkflowHelper.RecordHistoryAsync(UnitOfWork, claim.WarrantyClaimId, from, claim.Status, request.UserId, request.Notes);
            await UnitOfWork.SaveChangesAsync();
            await AuditService.LogActionAsync("WarrantyClaim", claim.WarrantyClaimId, GetActionName(), request.UserId, "Staff", claim.Status.ToString());
            return true;
        }

        protected abstract bool CanTransition(WarrantyClaim claim, T request);
        protected abstract void ApplyTransition(WarrantyClaim claim, T request);
        protected abstract string GetActionName();

        protected static DateTime ResolveActionDate(DateTime? userDate)
            => (userDate ?? DateTime.UtcNow).Date;
    }

    public abstract class WarrantyClaimFlagHandler<T> : IRequestHandler<T, bool> where T : WarrantyClaimActionCommand
    {
        protected readonly IUnitOfWork UnitOfWork;
        protected readonly IAuditService AuditService;

        protected WarrantyClaimFlagHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            UnitOfWork = unitOfWork;
            AuditService = auditService;
        }

        public async Task<bool> Handle(T request, CancellationToken cancellationToken)
        {
            var claim = await UnitOfWork.WarrantyClaims.GetByIdAsync(request.WarrantyClaimId);
            if (claim == null) return false;
            if (!CanApply(claim, request)) return false;

            var from = claim.Status;
            ApplyFlag(claim, request);
            WarrantyClaimWorkflowHelper.TryMarkComplete(claim);
            claim.ModifiedByUserId = request.UserId;
            claim.ModifiedDate = DateTime.UtcNow;
            await UnitOfWork.WarrantyClaims.UpdateAsync(claim);
            await WarrantyClaimWorkflowHelper.RecordHistoryAsync(
                UnitOfWork, claim.WarrantyClaimId, from, claim.Status, request.UserId, GetHistoryNote(claim, request));
            await UnitOfWork.SaveChangesAsync();
            await AuditService.LogActionAsync("WarrantyClaim", claim.WarrantyClaimId, GetActionName(), request.UserId, GetActorRole(request), claim.Status.ToString());
            return true;
        }

        protected virtual string GetActorRole(T request) => "Staff";
        protected abstract bool CanApply(WarrantyClaim claim, T request);
        protected abstract void ApplyFlag(WarrantyClaim claim, T request);
        protected abstract string GetActionName();
        protected abstract string? GetHistoryNote(WarrantyClaim claim, T request);

        protected static DateTime ResolveActionDate(DateTime? userDate)
            => (userDate ?? DateTime.UtcNow).Date;
    }

    public class AcceptWarrantyClaimCommandHandler : WarrantyClaimTransitionHandler<AcceptWarrantyClaimCommand>
    {
        public AcceptWarrantyClaimCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, AcceptWarrantyClaimCommand r) => WarrantyClaimStatus.CanAccept(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, AcceptWarrantyClaimCommand r)
        {
            c.Status = WarrantyClaimStatus.Accepted;
            c.ApprovedByUserId = r.UserId;
            c.ApprovedDate = DateTime.UtcNow;
            c.RejectionReason = null;
            c.MoreInfoNotes = null;
        }
        protected override string GetActionName() => "Accept";
    }

    public class RejectWarrantyClaimCommandHandler : WarrantyClaimTransitionHandler<RejectWarrantyClaimCommand>
    {
        public RejectWarrantyClaimCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, RejectWarrantyClaimCommand r)
            => WarrantyClaimStatus.CanStaffReview(c.Status) && !string.IsNullOrWhiteSpace(r.Notes);
        protected override void ApplyTransition(WarrantyClaim c, RejectWarrantyClaimCommand r)
        {
            c.Status = WarrantyClaimStatus.Rejected;
            c.RejectedByUserId = r.UserId;
            c.RejectedDate = DateTime.UtcNow;
            c.RejectionReason = r.Notes?.Trim();
        }
        protected override string GetActionName() => "Reject";
    }

    public class RequestWarrantyInfoCommandHandler : WarrantyClaimTransitionHandler<RequestWarrantyInfoCommand>
    {
        public RequestWarrantyInfoCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, RequestWarrantyInfoCommand r)
            => WarrantyClaimStatus.CanStaffReview(c.Status) && !string.IsNullOrWhiteSpace(r.Notes);
        protected override void ApplyTransition(WarrantyClaim c, RequestWarrantyInfoCommand r)
        {
            c.Status = WarrantyClaimStatus.MoreInfoRequested;
            c.MoreInfoRequestedByUserId = r.UserId;
            c.MoreInfoRequestedDate = DateTime.UtcNow;
            c.MoreInfoNotes = r.Notes?.Trim();
        }
        protected override string GetActionName() => "RequestInfo";
    }

    public class ApplyWarrantyToAmpereCommandHandler : WarrantyClaimTransitionHandler<ApplyWarrantyToAmpereCommand>
    {
        public ApplyWarrantyToAmpereCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, ApplyWarrantyToAmpereCommand r)
            => WarrantyClaimStatus.CanApplyToAmpere(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, ApplyWarrantyToAmpereCommand r)
        {
            c.Status = WarrantyClaimStatus.AppliedToAmpere;
            c.AmpereAppliedByUserId = r.UserId;
            c.AmpereAppliedDate = ResolveActionDate(r.ActionDate);
        }
        protected override string GetActionName() => "ApplyToAmpere";
    }

    public class MarkWarrantyAmpereApprovedCommandHandler : WarrantyClaimTransitionHandler<MarkWarrantyAmpereApprovedCommand>
    {
        public MarkWarrantyAmpereApprovedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, MarkWarrantyAmpereApprovedCommand r)
            => WarrantyClaimStatus.CanMarkAmpereApproved(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, MarkWarrantyAmpereApprovedCommand r)
        {
            c.Status = WarrantyClaimStatus.AmpereApproved;
            c.AmpereApprovedByUserId = r.UserId;
            c.AmpereApprovedDate = ResolveActionDate(r.ActionDate);
        }
        protected override string GetActionName() => "AmpereApproved";
    }

    public class UpdateWarrantySoNumberCommandHandler : IRequestHandler<UpdateWarrantySoNumberCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateWarrantySoNumberCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateWarrantySoNumberCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SoNumber))
                return false;

            var claim = await _unitOfWork.WarrantyClaims.GetByIdAsync(request.WarrantyClaimId);
            if (claim == null || !WarrantyClaimStatus.CanStaffEditSoNumber(claim.Status, claim.DefectiveSentToAmpereCompleted))
                return false;

            var fromStatus = claim.Status;
            claim.SoNumber = request.SoNumber.Trim().ToUpperInvariant();
            claim.ModifiedByUserId = request.UserId;
            claim.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.WarrantyClaims.UpdateAsync(claim);
            await WarrantyClaimWorkflowHelper.RecordHistoryAsync(
                _unitOfWork, claim.WarrantyClaimId, fromStatus, claim.Status, request.UserId,
                $"SO Number: {claim.SoNumber}");
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogActionAsync("WarrantyClaim", claim.WarrantyClaimId, "UpdateSoNumber", request.UserId, "Staff", claim.SoNumber);
            return true;
        }
    }

    public class SaveWarrantyResolutionPartCommandHandler : WarrantyClaimFlagHandler<SaveWarrantyResolutionPartCommand>
    {
        public SaveWarrantyResolutionPartCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanApply(WarrantyClaim c, SaveWarrantyResolutionPartCommand r)
        {
            if (!WarrantyClaimStatus.CanStaffEditResolutionPart(c.Status, c.DefectiveSentToAmpereCompleted))
                return false;
            if (string.IsNullOrWhiteSpace(r.DealerResolutionType)
                || !WarrantyDealerResolutionTypes.All.Contains(r.DealerResolutionType.Trim().ToUpperInvariant()))
                return false;
            return !string.IsNullOrWhiteSpace(r.DealerClosedPartNumber);
        }
        protected override void ApplyFlag(WarrantyClaim c, SaveWarrantyResolutionPartCommand r)
        {
            c.DealerResolutionType = r.DealerResolutionType.Trim().ToUpperInvariant();
            c.DealerClosedPartNumber = r.DealerClosedPartNumber.Trim().ToUpperInvariant();
            c.ResolutionPartCompleted = true;
            c.ResolutionPartCompletedByUserId = r.UserId;
            c.ResolutionPartCompletedDate = DateTime.UtcNow;
        }
        protected override string GetActionName() => "ResolutionPart";
        protected override string? GetHistoryNote(WarrantyClaim c, SaveWarrantyResolutionPartCommand r)
            => $"Resolution: {WarrantyDealerResolutionTypes.GetDisplayName(c.DealerResolutionType)} · Part # {c.DealerClosedPartNumber}";
    }

    public class SaveWarrantyDealerInvoiceClosedCommandHandler : WarrantyClaimFlagHandler<SaveWarrantyDealerInvoiceClosedCommand>
    {
        public SaveWarrantyDealerInvoiceClosedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanApply(WarrantyClaim c, SaveWarrantyDealerInvoiceClosedCommand r)
            => WarrantyClaimStatus.CanStaffEditDealerInvoiceClosed(c.Status, c.DefectiveSentToAmpereCompleted)
               && !string.IsNullOrWhiteSpace(r.DealerClosedInvoiceNumber);
        protected override void ApplyFlag(WarrantyClaim c, SaveWarrantyDealerInvoiceClosedCommand r)
        {
            c.DealerClosedInvoiceNumber = r.DealerClosedInvoiceNumber.Trim().ToUpperInvariant();
            c.DealerClosedDate = ResolveActionDate(r.ActionDate);
            c.DealerInvoiceClosedCompleted = true;
            c.DealerInvoiceClosedByUserId = r.UserId;
        }
        protected override string GetActionName() => "DealerInvoiceClosed";
        protected override string? GetHistoryNote(WarrantyClaim c, SaveWarrantyDealerInvoiceClosedCommand r)
            => $"Invoice # {c.DealerClosedInvoiceNumber} · Closed {c.DealerClosedDate:dd-MMM-yyyy}";
    }

    public class MarkWarrantyReplacementPartReceivedCommandHandler : WarrantyClaimFlagHandler<MarkWarrantyReplacementPartReceivedCommand>
    {
        public MarkWarrantyReplacementPartReceivedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanApply(WarrantyClaim c, MarkWarrantyReplacementPartReceivedCommand r)
            => WarrantyClaimStatus.CanStaffEditReplacementPartReceived(c.Status, c.DefectiveSentToAmpereCompleted);
        protected override void ApplyFlag(WarrantyClaim c, MarkWarrantyReplacementPartReceivedCommand r)
        {
            c.ReplacementPartReceivedCompleted = true;
            c.ProductReceivedByUserId = r.UserId;
            c.ProductReceivedDate = ResolveActionDate(r.ActionDate);
        }
        protected override string GetActionName() => "ReplacementPartReceived";
        protected override string? GetHistoryNote(WarrantyClaim c, MarkWarrantyReplacementPartReceivedCommand r)
            => $"Replacement part received {c.ProductReceivedDate:dd-MMM-yyyy}";
    }

    public class MarkWarrantySubdealerPartReceivedCommandHandler : WarrantyClaimFlagHandler<MarkWarrantySubdealerPartReceivedCommand>
    {
        public MarkWarrantySubdealerPartReceivedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanApply(WarrantyClaim c, MarkWarrantySubdealerPartReceivedCommand r)
        {
            if (string.IsNullOrWhiteSpace(r.ReceivedByName))
                return false;

            if (r.OnBehalfOfSubdealerByStaff)
                return WarrantyClaimStatus.CanStaffEditSubdealerPartReceived(c.Status, c.DefectiveSentToAmpereCompleted);

            return WarrantyClaimStatus.CanMarkSubdealerPartReceived(c.Status, c.SubdealerPartReceivedCompleted)
                   && c.AccountId == r.AccountId
                   && !c.SubdealerPartReceivedStaffUserId.HasValue;
        }

        protected override void ApplyFlag(WarrantyClaim c, MarkWarrantySubdealerPartReceivedCommand r)
        {
            c.SubdealerPartReceivedCompleted = true;
            c.CollectedByAccountId = c.AccountId;
            c.CollectedByName = r.ReceivedByName.Trim();
            c.CollectedDate = ResolveActionDate(r.ActionDate);
            c.SubdealerPartReceivedStaffUserId = r.OnBehalfOfSubdealerByStaff ? r.UserId : null;
        }

        protected override string GetActionName() => "SubdealerPartReceived";
        protected override string GetActorRole(MarkWarrantySubdealerPartReceivedCommand r)
            => r.OnBehalfOfSubdealerByStaff ? "Staff" : "Subdealer";
        protected override string? GetHistoryNote(WarrantyClaim c, MarkWarrantySubdealerPartReceivedCommand r)
            => (r.OnBehalfOfSubdealerByStaff ? "Staff recorded part received: " : "Received by ")
               + $"{c.CollectedByName} on {c.CollectedDate:dd-MMM-yyyy}";
    }

    public class MarkWarrantyDefectiveHandoverCommandHandler : WarrantyClaimFlagHandler<MarkWarrantyDefectiveHandoverCommand>
    {
        public MarkWarrantyDefectiveHandoverCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanApply(WarrantyClaim c, MarkWarrantyDefectiveHandoverCommand r)
        {
            if (string.IsNullOrWhiteSpace(r.HandoverByName))
                return false;

            if (r.OnBehalfOfSubdealerByStaff)
                return WarrantyClaimStatus.CanStaffEditDefectiveHandover(c.Status, c.DefectiveSentToAmpereCompleted);

            return WarrantyClaimStatus.CanMarkDefectiveHandover(c.Status, c.DefectiveHandoverCompleted)
                   && c.AccountId == r.AccountId
                   && !c.DefectiveHandoverStaffUserId.HasValue;
        }

        protected override void ApplyFlag(WarrantyClaim c, MarkWarrantyDefectiveHandoverCommand r)
        {
            c.DefectiveHandoverCompleted = true;
            c.DefectiveSubmittedByAccountId = c.AccountId;
            c.DefectiveSubmittedByName = r.HandoverByName.Trim();
            c.DefectiveSubmittedDate = ResolveActionDate(r.ActionDate);
            c.DefectiveHandoverStaffUserId = r.OnBehalfOfSubdealerByStaff ? r.UserId : null;
        }

        protected override string GetActionName() => "DefectiveHandover";
        protected override string GetActorRole(MarkWarrantyDefectiveHandoverCommand r)
            => r.OnBehalfOfSubdealerByStaff ? "Staff" : "Subdealer";
        protected override string? GetHistoryNote(WarrantyClaim c, MarkWarrantyDefectiveHandoverCommand r)
            => (r.OnBehalfOfSubdealerByStaff ? "Staff recorded defective handover: " : "Defective handover by ")
               + $"{c.DefectiveSubmittedByName} on {c.DefectiveSubmittedDate:dd-MMM-yyyy}";
    }

    public class MarkWarrantyDefectiveSentToAmpereCommandHandler : WarrantyClaimFlagHandler<MarkWarrantyDefectiveSentToAmpereCommand>
    {
        public MarkWarrantyDefectiveSentToAmpereCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanApply(WarrantyClaim c, MarkWarrantyDefectiveSentToAmpereCommand r)
            => WarrantyClaimStatus.CanStaffMarkDefectiveSentToAmpere(c.Status, c.DefectiveSentToAmpereCompleted);
        protected override void ApplyFlag(WarrantyClaim c, MarkWarrantyDefectiveSentToAmpereCommand r)
        {
            c.DefectiveSentToAmpereCompleted = true;
            c.DefectiveSentToAmpereByUserId = r.UserId;
            c.DefectiveSentToAmpereDate = ResolveActionDate(r.ActionDate);
        }
        protected override string GetActionName() => "DefectiveSentToAmpere";
        protected override string? GetHistoryNote(WarrantyClaim c, MarkWarrantyDefectiveSentToAmpereCommand r)
            => $"Defective sent to Ampere on {c.DefectiveSentToAmpereDate:dd-MMM-yyyy}";
    }
}

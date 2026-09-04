using MediatR;
using KRSDealerManagement.Application.Commands;
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
            claim.ClaimType = request.ClaimType.Trim().ToUpperInvariant();
            claim.AccountId = request.AccountId;
            claim.SubdealerId = request.SubdealerId;
            claim.DealershipId = request.DealershipId;
            claim.SubdealerVehicleId = request.SubdealerVehicleId;
            claim.ChassisNo = request.ChassisNo.Trim().ToUpperInvariant();
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
            claim.WarrantyPartId = request.WarrantyPartId;
            claim.PartCode = TrimUpper(request.PartCode);
            claim.FailurePartSerialNumber = TrimUpper(request.FailurePartSerialNumber);
            claim.CustomerComplaint = Trim(request.CustomerComplaint);
            claim.DealerObservation = Trim(request.DealerObservation);
            claim.Remarks = Trim(request.Remarks);
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
    }

    public class ApproveWarrantyClaimCommandHandler : WarrantyClaimTransitionHandler<ApproveWarrantyClaimCommand>
    {
        public ApproveWarrantyClaimCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, ApproveWarrantyClaimCommand r) => WarrantyClaimStatus.CanStaffReview(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, ApproveWarrantyClaimCommand r)
        {
            c.Status = WarrantyClaimStatus.Approved;
            c.ApprovedByUserId = r.UserId;
            c.ApprovedDate = DateTime.UtcNow;
            c.RejectionReason = null;
            c.MoreInfoNotes = null;
        }
        protected override string GetActionName() => "Approve";
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
        protected override bool CanTransition(WarrantyClaim c, ApplyWarrantyToAmpereCommand r) => WarrantyClaimStatus.CanApplyToAmpere(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, ApplyWarrantyToAmpereCommand r)
        {
            c.Status = WarrantyClaimStatus.AppliedToAmpere;
            c.AmpereAppliedByUserId = r.UserId;
            c.AmpereAppliedDate = DateTime.UtcNow;
        }
        protected override string GetActionName() => "ApplyToAmpere";
    }

    public class MarkWarrantyProductReceivedCommandHandler : WarrantyClaimTransitionHandler<MarkWarrantyProductReceivedCommand>
    {
        public MarkWarrantyProductReceivedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, MarkWarrantyProductReceivedCommand r) => WarrantyClaimStatus.CanMarkProductReceived(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, MarkWarrantyProductReceivedCommand r)
        {
            c.Status = WarrantyClaimStatus.ProductReceived;
            c.ProductReceivedByUserId = r.UserId;
            c.ProductReceivedDate = DateTime.UtcNow;
        }
        protected override string GetActionName() => "ProductReceived";
    }

    public class MarkWarrantyCollectedCommandHandler : WarrantyClaimTransitionHandler<MarkWarrantyCollectedCommand>
    {
        public MarkWarrantyCollectedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, MarkWarrantyCollectedCommand r)
            => WarrantyClaimStatus.CanSubdealerCollect(c.Status) && c.AccountId == r.AccountId;
        protected override void ApplyTransition(WarrantyClaim c, MarkWarrantyCollectedCommand r)
        {
            c.Status = WarrantyClaimStatus.CollectedBySubdealer;
            c.CollectedByAccountId = r.AccountId;
            c.CollectedDate = DateTime.UtcNow;
        }
        protected override string GetActionName() => "Collected";
    }

    public class MarkWarrantyDefectiveSubmittedCommandHandler : WarrantyClaimTransitionHandler<MarkWarrantyDefectiveSubmittedCommand>
    {
        public MarkWarrantyDefectiveSubmittedCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, MarkWarrantyDefectiveSubmittedCommand r)
            => WarrantyClaimStatus.CanSubdealerSubmitDefective(c.Status) && c.AccountId == r.AccountId;
        protected override void ApplyTransition(WarrantyClaim c, MarkWarrantyDefectiveSubmittedCommand r)
        {
            c.Status = WarrantyClaimStatus.DefectiveSubmitted;
            c.DefectiveSubmittedByAccountId = r.AccountId;
            c.DefectiveSubmittedDate = DateTime.UtcNow;
        }
        protected override string GetActionName() => "DefectiveSubmitted";
    }

    public class MarkWarrantyDefectiveSentToAmpereCommandHandler : WarrantyClaimTransitionHandler<MarkWarrantyDefectiveSentToAmpereCommand>
    {
        public MarkWarrantyDefectiveSentToAmpereCommandHandler(IUnitOfWork u, IAuditService a) : base(u, a) { }
        protected override bool CanTransition(WarrantyClaim c, MarkWarrantyDefectiveSentToAmpereCommand r) => WarrantyClaimStatus.CanMarkDefectiveSentToAmpere(c.Status);
        protected override void ApplyTransition(WarrantyClaim c, MarkWarrantyDefectiveSentToAmpereCommand r)
        {
            c.Status = WarrantyClaimStatus.DefectiveSentToAmpere;
            c.DefectiveSentToAmpereByUserId = r.UserId;
            c.DefectiveSentToAmpereDate = DateTime.UtcNow;
        }
        protected override string GetActionName() => "DefectiveSentToAmpere";
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetWarrantyClaimsQueryHandler : IRequestHandler<GetWarrantyClaimsQuery, IEnumerable<WarrantyClaimDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetWarrantyClaimsQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<WarrantyClaimDto>> Handle(GetWarrantyClaimsQuery request, CancellationToken cancellationToken)
        {
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Warranty);
            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToDictionary(a => a.AccountId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var parts = (await _unitOfWork.WarrantyParts.GetAllAsync()).ToDictionary(p => p.WarrantyPartId);

            var claims = (await _unitOfWork.WarrantyClaims.GetAllAsync()).AsEnumerable();

            if (request.Status.HasValue)
                claims = claims.Where(c => c.Status == request.Status.Value);
            if (request.ExcludeDraft)
                claims = claims.Where(c => c.Status != WarrantyClaimStatus.Draft);
            if (request.ExcludeComplete)
                claims = claims.Where(c => c.Status != WarrantyClaimStatus.Complete);
            if (request.OnlyComplete)
                claims = claims.Where(c => c.Status == WarrantyClaimStatus.Complete);
            if (request.CompletedFromDate.HasValue)
            {
                var from = request.CompletedFromDate.Value.Date;
                claims = claims.Where(c => c.CompletedDate.HasValue && c.CompletedDate.Value.Date >= from);
            }
            if (request.CompletedToDate.HasValue)
            {
                var to = request.CompletedToDate.Value.Date;
                claims = claims.Where(c => c.CompletedDate.HasValue && c.CompletedDate.Value.Date <= to);
            }
            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
            if (dealershipFilter != null)
                claims = claims.Where(c => c.DealershipId.HasValue && dealershipFilter.Contains(c.DealershipId.Value));
            if (request.AccountId.HasValue)
                claims = claims.Where(c => c.AccountId == request.AccountId.Value);
            if (!string.IsNullOrWhiteSpace(request.ClaimType))
                claims = claims.Where(c => c.ClaimType.Equals(request.ClaimType, StringComparison.OrdinalIgnoreCase));

            if (request.SubdealerUserId.HasValue)
            {
                var orgUserIds = await SubdealerOrgService.GetOrgLoginUserIdsAsync(_unitOfWork, request.SubdealerUserId.Value);
                var accountIds = (await _unitOfWork.SubdealerAccounts.GetAllAsync())
                    .Where(a => orgUserIds.Contains(a.SubdealerId) || a.SubdealerId == request.SubdealerUserId.Value)
                    .Select(a => a.AccountId)
                    .ToHashSet();
                claims = claims.Where(c => accountIds.Contains(c.AccountId));
            }

            return claims
                .OrderByDescending(c => c.SubmittedDate ?? c.CreatedDate)
                .Select(c =>
                {
                    accounts.TryGetValue(c.AccountId, out var account);
                    users.TryGetValue(c.SubdealerId, out var subUser);
                    statusMap.TryGetValue(c.Status, out var st);
                    dealerships.TryGetValue(c.DealershipId ?? 0, out var dealer);
                    parts.TryGetValue(c.WarrantyPartId ?? 0, out var part);

                    return new WarrantyClaimDto
                    {
                        WarrantyClaimId = c.WarrantyClaimId,
                        ClaimNumber = c.ClaimNumber,
                        ClaimType = c.ClaimType,
                        Status = c.Status,
                        StatusName = st?.StatusName,
                        StatusBadgeClass = st?.BadgeClass,
                        AccountId = c.AccountId,
                        AccountName = subUser?.GetFullName() ?? account?.AccountName,
                        SubdealerId = c.SubdealerId,
                        DealershipId = c.DealershipId,
                        DealershipName = dealer?.DealershipName,
                        ChassisNo = c.ChassisNo,
                        CustomerName = c.CustomerName,
                        PartName = WarrantyPartHelper.ResolveDisplayName(part, c.OtherPartName),
                        CurrentKms = c.CurrentKms,
                        SubmittedDate = c.SubmittedDate,
                        CompletedDate = c.CompletedDate,
                        CreatedDate = c.CreatedDate,
                        ModifiedDate = c.ModifiedDate
                    };
                })
                .ToList();
        }
    }

    public class GetWarrantyClaimDetailQueryHandler : IRequestHandler<GetWarrantyClaimDetailQuery, WarrantyClaimDetailDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetWarrantyClaimDetailQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<WarrantyClaimDetailDto?> Handle(GetWarrantyClaimDetailQuery request, CancellationToken cancellationToken)
        {
            var claim = await _unitOfWork.WarrantyClaims.GetByIdAsync(request.WarrantyClaimId);
            if (claim == null) return null;

            if (!request.IsSystemAdmin)
            {
                if (request.AccountId.HasValue && claim.AccountId != request.AccountId.Value)
                    return null;
                var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
                if (dealershipFilter != null
                    && claim.DealershipId.HasValue
                    && !DealershipQueryScope.MatchesDealership(claim.DealershipId.Value, dealershipFilter))
                    return null;
            }

            var statusMap = await _statuses.GetMapAsync(StatusCategories.Warranty);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToDictionary(a => a.AccountId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var parts = (await _unitOfWork.WarrantyParts.GetAllAsync()).ToDictionary(p => p.WarrantyPartId);

            accounts.TryGetValue(claim.AccountId, out var account);
            users.TryGetValue(claim.SubdealerId, out var subUser);
            statusMap.TryGetValue(claim.Status, out var st);
            dealerships.TryGetValue(claim.DealershipId ?? 0, out var dealer);
            parts.TryGetValue(claim.WarrantyPartId ?? 0, out var part);

            string Name(int? id) => id.HasValue && users.TryGetValue(id.Value, out var u) ? u.GetFullName() : "";
            string AccountName(int? id) => id.HasValue && accounts.TryGetValue(id.Value, out var a) ? a.AccountName : "";

            return new WarrantyClaimDetailDto
            {
                WarrantyClaimId = claim.WarrantyClaimId,
                ClaimNumber = claim.ClaimNumber,
                ClaimType = claim.ClaimType,
                Status = claim.Status,
                StatusName = st?.StatusName,
                StatusBadgeClass = st?.BadgeClass,
                AccountId = claim.AccountId,
                AccountName = subUser?.GetFullName() ?? account?.AccountName,
                SubdealerId = claim.SubdealerId,
                DealershipId = claim.DealershipId,
                DealershipName = dealer?.DealershipName,
                ChassisNo = claim.ChassisNo,
                SubdealerVehicleId = claim.SubdealerVehicleId,
                CustomerName = claim.CustomerName,
                CustomerMobile = claim.CustomerMobile,
                ContactPerson = claim.ContactPerson,
                ContactMobile = claim.ContactMobile,
                ModelId = claim.ModelId,
                ModelName = claim.ModelName,
                ColorId = claim.ColorId,
                ColorName = claim.ColorName,
                CurrentKms = claim.CurrentKms,
                SaleDate = claim.SaleDate,
                ComplaintDate = claim.ComplaintDate,
                WarrantyPartId = claim.WarrantyPartId,
                PartName = WarrantyPartHelper.ResolveDisplayName(part, claim.OtherPartName),
                OtherPartName = claim.OtherPartName,
                PartCode = claim.PartCode,
                FailurePartSerialNumber = claim.FailurePartSerialNumber,
                CustomerComplaint = claim.CustomerComplaint,
                DealerObservation = claim.DealerObservation,
                Remarks = claim.Remarks,
                RejectionReason = claim.RejectionReason,
                MoreInfoNotes = claim.MoreInfoNotes,
                SoNumber = claim.SoNumber,
                SubmittedDate = claim.SubmittedDate,
                CompletedDate = claim.CompletedDate,
                CreatedDate = claim.CreatedDate,
                ModifiedDate = claim.ModifiedDate,
                AmpereAppliedDate = claim.AmpereAppliedDate,
                AmpereAppliedByName = Name(claim.AmpereAppliedByUserId),
                AmpereApprovedDate = claim.AmpereApprovedDate,
                AmpereApprovedByName = Name(claim.AmpereApprovedByUserId),
                AcceptedDate = claim.ApprovedDate,
                AcceptedByName = Name(claim.ApprovedByUserId),
                DealerResolutionType = claim.DealerResolutionType,
                DealerResolutionTypeName = WarrantyDealerResolutionTypes.GetDisplayName(claim.DealerResolutionType),
                DealerClosedPartNumber = claim.DealerClosedPartNumber,
                DealerClosedDate = claim.DealerClosedDate,
                DealerClosedInvoiceNumber = claim.DealerClosedInvoiceNumber,
                ResolutionPartCompleted = claim.ResolutionPartCompleted,
                DealerInvoiceClosedCompleted = claim.DealerInvoiceClosedCompleted,
                ReplacementPartReceivedCompleted = claim.ReplacementPartReceivedCompleted,
                SubdealerPartReceivedCompleted = claim.SubdealerPartReceivedCompleted,
                DefectiveHandoverCompleted = claim.DefectiveHandoverCompleted,
                DefectiveSentToAmpereCompleted = claim.DefectiveSentToAmpereCompleted,
                SubdealerPartReceivedLockedByStaff = claim.SubdealerPartReceivedStaffUserId.HasValue,
                DefectiveHandoverLockedByStaff = claim.DefectiveHandoverStaffUserId.HasValue,
                ReplacementPartReceivedDate = claim.ProductReceivedDate,
                ReplacementPartReceivedByName = Name(claim.ProductReceivedByUserId),
                SubdealerPartReceivedDate = claim.CollectedDate,
                SubdealerPartReceivedByName = claim.CollectedByName,
                DefectiveHandoverDate = claim.DefectiveSubmittedDate,
                DefectiveHandoverByName = claim.DefectiveSubmittedByName,
                DefectiveSentToAmpereDate = claim.DefectiveSentToAmpereDate,
                DefectiveSentToAmpereByName = Name(claim.DefectiveSentToAmpereByUserId),
                ServiceEntries = (await _unitOfWork.WarrantyClaimServiceEntries.GetAllAsync())
                    .Where(e => e.WarrantyClaimId == claim.WarrantyClaimId)
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new WarrantyClaimServiceEntryDto
                    {
                        ServiceEntryId = e.ServiceEntryId,
                        ServiceType = e.ServiceType,
                        ServiceDate = e.ServiceDate,
                        ServiceKms = e.ServiceKms,
                        SortOrder = e.SortOrder
                    }).ToList(),
                Attachments = (await _unitOfWork.WarrantyClaimAttachments.GetAllAsync())
                    .Where(a => a.WarrantyClaimId == claim.WarrantyClaimId && a.IsActive)
                    .Select(a => new WarrantyClaimAttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        AttachmentType = a.AttachmentType,
                        AttachmentTypeName = WarrantyAttachmentTypes.GetDisplayName(a.AttachmentType),
                        FilePath = a.FilePath,
                        OriginalFileName = a.OriginalFileName,
                        UploadedDate = a.UploadedDate
                    }).ToList(),
                History = (await _unitOfWork.WarrantyClaimStatusHistories.GetAllAsync())
                    .Where(h => h.WarrantyClaimId == claim.WarrantyClaimId)
                    .OrderByDescending(h => h.ChangedDate)
                    .Select(h => new WarrantyClaimHistoryDto
                    {
                        HistoryId = h.HistoryId,
                        FromStatus = h.FromStatus,
                        FromStatusName = h.FromStatus.HasValue && statusMap.TryGetValue(h.FromStatus.Value, out var fs) ? fs.StatusName : null,
                        ToStatus = h.ToStatus,
                        ToStatusName = statusMap.TryGetValue(h.ToStatus, out var ts) ? ts.StatusName : null,
                        ChangedByName = Name(h.ChangedByUserId),
                        ChangedDate = h.ChangedDate,
                        Notes = h.Notes
                    }).ToList()
            };
        }
    }

    public class GetWarrantyChassisLookupQueryHandler : IRequestHandler<GetWarrantyChassisLookupQuery, WarrantyChassisLookupDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetWarrantyChassisLookupQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<WarrantyChassisLookupDto?> Handle(GetWarrantyChassisLookupQuery request, CancellationToken cancellationToken)
        {
            var chassis = request.ChassisNo.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(chassis)) return null;

            var master = await _unitOfWork.VehicleMasters.GetByChassisAsync(chassis);
            if (master == null)
            {
                return new WarrantyChassisLookupDto
                {
                    ChassisNo = chassis,
                    FoundInMaster = false
                };
            }

            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            models.TryGetValue(master.ModelId, out var model);
            colors.TryGetValue(master.ColorId, out var color);

            var dto = new WarrantyChassisLookupDto
            {
                VehicleMasterId = master.VehicleMasterId,
                ChassisNo = chassis,
                ModelId = master.ModelId,
                ModelName = model?.ModelName,
                ColorId = master.ColorId,
                ColorName = color?.ColorName,
                FoundInMaster = true
            };

            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToDictionary(b => b.VehicleId);
            var vehicle = (await _unitOfWork.Vehicles.GetAllAsync())
                .FirstOrDefault(v =>
                    v.ChassisNumber != null
                    && v.ChassisNumber.Equals(chassis, StringComparison.OrdinalIgnoreCase));

            if (vehicle != null)
            {
                bookings.TryGetValue(vehicle.VehicleId, out var booking);
                dto.VehicleId = vehicle.VehicleId;
                dto.CustomerName = booking?.CustomerName;
                dto.CustomerMobile = booking?.CustomerMobile;
                dto.SaleDate = booking?.SubmittedDate;
            }

            return dto;
        }
    }

    public class SearchWarrantyChassisQueryHandler : IRequestHandler<SearchWarrantyChassisQuery, IEnumerable<WarrantyChassisOptionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SearchWarrantyChassisQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<WarrantyChassisOptionDto>> Handle(SearchWarrantyChassisQuery request, CancellationToken cancellationToken)
        {
            var term = request.Term?.Trim().ToUpperInvariant() ?? "";
            var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 100);

            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);

            var query = (await _unitOfWork.VehicleMasters.GetAllAsync()).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(m => m.ChassisNumber.Contains(term, StringComparison.OrdinalIgnoreCase));

            return query
                .OrderBy(m => m.ChassisNumber)
                .Take(take)
                .Select(m =>
                {
                    models.TryGetValue(m.ModelId, out var model);
                    colors.TryGetValue(m.ColorId, out var color);
                    var modelName = model?.ModelName ?? "";
                    var colorName = color?.ColorName ?? "";
                    return new WarrantyChassisOptionDto
                    {
                        VehicleMasterId = m.VehicleMasterId,
                        ChassisNo = m.ChassisNumber,
                        ModelId = m.ModelId,
                        ModelName = modelName,
                        ColorId = m.ColorId,
                        ColorName = colorName,
                        Label = m.ChassisNumber
                    };
                })
                .ToList();
        }
    }
}

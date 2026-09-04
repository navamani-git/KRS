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
            if (request.DealershipId.HasValue)
                claims = claims.Where(c => c.DealershipId == request.DealershipId.Value);
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
                        AccountName = account?.AccountName ?? subUser?.GetFullName(),
                        SubdealerId = c.SubdealerId,
                        DealershipId = c.DealershipId,
                        DealershipName = dealer?.DealershipName,
                        ChassisNo = c.ChassisNo,
                        CustomerName = c.CustomerName,
                        PartName = WarrantyPartHelper.ResolveDisplayName(part, c.OtherPartName),
                        CurrentKms = c.CurrentKms,
                        SubmittedDate = c.SubmittedDate,
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
                if (request.DealershipId.HasValue && claim.DealershipId != request.DealershipId.Value)
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
                AccountName = account?.AccountName ?? subUser?.GetFullName(),
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
                CreatedDate = claim.CreatedDate,
                ModifiedDate = claim.ModifiedDate,
                AmpereAppliedDate = claim.AmpereAppliedDate,
                AmpereAppliedByName = Name(claim.AmpereAppliedByUserId),
                ProductReceivedDate = claim.ProductReceivedDate,
                ProductReceivedByName = Name(claim.ProductReceivedByUserId),
                CollectedDate = claim.CollectedDate,
                CollectedByName = AccountName(claim.CollectedByAccountId),
                DefectiveSubmittedDate = claim.DefectiveSubmittedDate,
                DefectiveSubmittedByName = AccountName(claim.DefectiveSubmittedByAccountId),
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

            var orgUserIds = await SubdealerOrgService.GetOrgLoginUserIdsAsync(_unitOfWork, request.SubdealerUserId);
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToDictionary(b => b.VehicleId);

            var vehicle = (await _unitOfWork.Vehicles.GetAllAsync())
                .FirstOrDefault(v =>
                    v.ChassisNumber != null
                    && v.ChassisNumber.Equals(chassis, StringComparison.OrdinalIgnoreCase)
                    && v.SubdealerId.HasValue
                    && (orgUserIds.Contains(v.SubdealerId.Value) || v.SubdealerId.Value == request.SubdealerUserId)
                    && v.Status >= UnifiedVehicleStatus.BookedToCustomer);

            if (vehicle == null)
            {
                return new WarrantyChassisLookupDto
                {
                    ChassisNo = chassis,
                    IsKnownSoldVehicle = false
                };
            }

            models.TryGetValue(vehicle.ModelId, out var model);
            colors.TryGetValue(vehicle.ColorId, out var color);
            bookings.TryGetValue(vehicle.VehicleId, out var booking);

            return new WarrantyChassisLookupDto
            {
                VehicleId = vehicle.VehicleId,
                ChassisNo = chassis,
                ModelId = vehicle.ModelId,
                ModelName = model?.ModelName,
                ColorId = vehicle.ColorId,
                ColorName = color?.ColorName,
                CustomerName = booking?.CustomerName,
                CustomerMobile = booking?.CustomerMobile,
                SaleDate = booking?.SubmittedDate,
                IsKnownSoldVehicle = true
            };
        }
    }
}

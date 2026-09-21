using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetCommissionsQueryHandler : IRequestHandler<GetCommissionsQuery, IEnumerable<CommissionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetCommissionsQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<CommissionDto>> Handle(GetCommissionsQuery request, CancellationToken cancellationToken)
        {
            var commissions = await _unitOfWork.Commissions.GetAllAsync();
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var userOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var vehicles = await _unitOfWork.Vehicles.GetAllAsync();
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Commission);

            var result = from c in commissions
                         join a in accounts on c.SubdealerId equals a.SubdealerId into accGroup
                         from acc in accGroup.DefaultIfEmpty()
                         join v in vehicles on c.VehicleId equals v.VehicleId into vehicleGroup
                         from vehicle in vehicleGroup.DefaultIfEmpty()
                         select new CommissionDto
                         {
                             CommissionId = c.CommissionId,
                             AccountId = acc != null ? acc.AccountId : c.AccountId,
                             AccountName = acc != null ? acc.AccountName : "Unknown",
                             SubdealerId = c.SubdealerId,
                             SubdealerName = SubdealerOrgService.ResolveDisplayName(c.SubdealerId, userOrgRoles, orgs, users),
                             VehicleId = c.VehicleId,
                             VehicleChassisNumber = vehicle != null ? vehicle.ChassisNumber : "Unknown",
                             Month = c.Month,
                             Year = c.Year,
                             CommissionAmount = c.CommissionAmount,
                             Status = c.Status,
                             StatusName = statusMap.TryGetValue(c.Status, out var st) ? st.StatusName : null,
                             StatusBadgeClass = statusMap.TryGetValue(c.Status, out st) ? st.BadgeClass : null,
                             Notes = c.Notes,
                             ApprovedBy = c.ApprovedBy,
                             ApprovedByName = null,
                             ApprovedDate = c.ApprovedDate,
                             PaidDate = c.PaidDate,
                             RejectedBy = c.RejectedBy,
                             RejectedDate = c.RejectedDate,
                             CreatedDate = c.CreatedDate,
                             ModifiedDate = c.ModifiedDate
                         };

            var list = result.ToList();
            foreach (var row in list)
            {
                if (row.ApprovedBy.HasValue && users.TryGetValue(row.ApprovedBy.Value, out var approver))
                    row.ApprovedByName = approver.GetFullName();
                if (row.RejectedBy.HasValue && users.TryGetValue(row.RejectedBy.Value, out var rejector))
                    row.RejectedByName = rejector.GetFullName();
            }

            result = list;

            if (request.SubdealerId.HasValue)
            {
                var orgId = await SubdealerOrgService.ResolveOrgIdAsync(_unitOfWork, request.SubdealerId.Value);
                result = result.Where(c => c.SubdealerId == orgId);
            }

            if (request.AccountId.HasValue)
                result = result.Where(c => c.AccountId == request.AccountId.Value);

            if (request.Status.HasValue)
                result = result.Where(c => c.Status == request.Status.Value);

            if (request.Month.HasValue)
                result = result.Where(c => c.Month == request.Month.Value);

            if (request.Year.HasValue)
                result = result.Where(c => c.Year == request.Year.Value);

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                result = result.Where(c => c.CreatedDate >= from);
            }

            if (request.ToDate.HasValue)
            {
                var toExclusive = request.ToDate.Value.Date.AddDays(1);
                result = result.Where(c => c.CreatedDate < toExclusive);
            }

            return result.OrderByDescending(c => c.Year)
                         .ThenByDescending(c => c.Month)
                         .ThenByDescending(c => c.CreatedDate)
                         .ToList();
        }
    }
}

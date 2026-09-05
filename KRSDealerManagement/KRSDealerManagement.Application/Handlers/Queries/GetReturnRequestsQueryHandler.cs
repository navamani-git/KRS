using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetReturnRequestsQueryHandler : IRequestHandler<GetReturnRequestsQuery, IEnumerable<ReturnRequestDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetReturnRequestsQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<ReturnRequestDto>> Handle(GetReturnRequestsQuery request, CancellationToken cancellationToken)
        {
            var returns = await _unitOfWork.ReturnRequests.GetAllAsync();
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var orders = await _unitOfWork.PurchaseOrders.GetAllAsync();
            var vehicles = await _unitOfWork.Vehicles.GetAllAsync();
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);
            var refundCredits = (await _unitOfWork.AccountTransactions.GetAllAsync())
                .Where(t => t.ReferenceType == "ReturnRequest"
                    && t.ReferenceId.HasValue
                    && AccountTransactionTypeHelper.IsCredit(t.TransactionType))
                .GroupBy(t => t.ReferenceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedDate).First());
            var bookedVehicleIds = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .Select(b => b.VehicleId)
                .ToHashSet();
            var masters = (await _unitOfWork.VehicleMasters.GetAllAsync()).ToDictionary(m => m.VehicleMasterId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var orgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();

            var scopedUserIds = request.DealershipId.HasValue
                ? (await _unitOfWork.UserOrgRoles.GetAllAsync())
                    .Where(a => a.IsActive && a.DealershipId == request.DealershipId.Value)
                    .Select(a => a.UserId)
                    .ToHashSet()
                : null;

            var result = from r in returns
                         join a in accounts on r.AccountId equals a.AccountId into accGroup
                         from acc in accGroup.DefaultIfEmpty()
                         join o in orders on r.OrderId equals o.OrderId into orderGroup
                         from ord in orderGroup.DefaultIfEmpty()
                         join v in vehicles on r.VehicleId equals v.VehicleId into vehicleGroup
                         from veh in vehicleGroup.DefaultIfEmpty()
                         join u in users.Values on r.ProcessedBy equals u.UserId into userGroup
                         from processedUser in userGroup.DefaultIfEmpty()
                         let subdealerUserId = veh?.SubdealerId ?? ord?.SubdealerId ?? acc?.SubdealerId
                         let displayStatus = VehicleStatusResolver.ResolveReturnDisplayStatus(r, veh)
                         select new ReturnRequestDto
                         {
                             ReturnRequestId = r.ReturnRequestId,
                             AccountId = r.AccountId,
                             AccountName = acc != null ? acc.AccountName : "Unknown",
                             SubdealerName = subdealerUserId.HasValue && users.TryGetValue(subdealerUserId.Value, out var subdealerUser)
                                 ? subdealerUser.GetFullName()
                                 : "Unknown",
                             DealershipLocation = DealershipLocationHelper.ResolveShowroomLabel(
                                 veh,
                                 ord?.SubdealerId,
                                 acc?.SubdealerId,
                                 masters,
                                 dealerships,
                                 orgRoles),
                             OrderId = r.OrderId,
                             OrderNumber = ord != null ? ord.OrderNumber : $"Order #{r.OrderId}",
                             VehicleId = r.VehicleId,
                             VehicleChassisNumber = veh != null ? veh.ChassisNumber : "Unknown",
                             SubdealerUserId = subdealerUserId,
                             RefundAmount = r.RefundAmount,
                             Status = displayStatus,
                             StatusName = statusMap.TryGetValue(displayStatus, out var st) ? st.StatusName : null,
                             StatusBadgeClass = statusMap.TryGetValue(displayStatus, out st) ? st.BadgeClass : null,
                             ReturnReason = r.ReturnReason,
                             AdminRemarks = r.AdminRemarks,
                             ProcessedBy = r.ProcessedBy,
                             ProcessedByName = processedUser != null ? processedUser.GetFullName() : null,
                             ProcessedDate = r.ProcessedDate,
                             RefundCreditedDate = refundCredits.TryGetValue(r.ReturnRequestId, out var creditTx)
                                 ? creditTx.CreatedDate
                                 : null,
                             CreatedDate = r.CreatedDate,
                             ModifiedDate = r.ModifiedDate,
                             CanAllocateToSubdealer = false
                         };

            if (request.ReturnRequestId.HasValue)
                result = result.Where(r => r.ReturnRequestId == request.ReturnRequestId.Value);

            if (request.AccountId.HasValue)
                result = result.Where(r => r.AccountId == request.AccountId.Value);

            if (request.SubdealerId.HasValue)
            {
                var orgUserIds = await SubdealerOrgService.GetOrgLoginUserIdsAsync(_unitOfWork, request.SubdealerId.Value);
                var subdealerAccountIds = accounts
                    .Where(a => orgUserIds.Contains(a.SubdealerId))
                    .Select(a => a.AccountId)
                    .ToHashSet();
                var vehicleSubdealerById = vehicles.ToDictionary(v => v.VehicleId, v => v.SubdealerId);
                var orderSubdealerById = orders.ToDictionary(o => o.OrderId, o => o.SubdealerId);

                result = result.Where(r =>
                    subdealerAccountIds.Contains(r.AccountId)
                    || (vehicleSubdealerById.TryGetValue(r.VehicleId, out var vehicleSubdealerId)
                        && vehicleSubdealerId.HasValue
                        && orgUserIds.Contains(vehicleSubdealerId.Value))
                    || (orderSubdealerById.TryGetValue(r.OrderId, out var orderSubdealerId)
                        && orgUserIds.Contains(orderSubdealerId))
                    || (r.SubdealerUserId.HasValue && orgUserIds.Contains(r.SubdealerUserId.Value)));
            }

            if (scopedUserIds != null)
            {
                var vehicleSubdealerById = vehicles.ToDictionary(v => v.VehicleId, v => v.SubdealerId);
                var orderSubdealerById = orders.ToDictionary(o => o.OrderId, o => o.SubdealerId);
                var accountSubdealerById = accounts.ToDictionary(a => a.AccountId, a => a.SubdealerId);

                result = result.Where(r =>
                    (vehicleSubdealerById.TryGetValue(r.VehicleId, out var vehicleSubdealerId)
                        && vehicleSubdealerId.HasValue
                        && scopedUserIds.Contains(vehicleSubdealerId.Value))
                    || (orderSubdealerById.TryGetValue(r.OrderId, out var orderSubdealerId)
                        && scopedUserIds.Contains(orderSubdealerId))
                    || (accountSubdealerById.TryGetValue(r.AccountId, out var accountSubdealerId)
                        && scopedUserIds.Contains(accountSubdealerId))
                    || (r.SubdealerUserId.HasValue && scopedUserIds.Contains(r.SubdealerUserId.Value)));
            }

            if (request.Status.HasValue)
                result = result.Where(r => r.Status == request.Status.Value);

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                result = result.Where(r => r.CreatedDate >= from);
            }

            if (request.ToDate.HasValue)
            {
                var toExclusive = request.ToDate.Value.Date.AddDays(1);
                result = result.Where(r => r.CreatedDate < toExclusive);
            }

            return result.OrderByDescending(r => r.CreatedDate).ToList();
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
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
            var users = await _unitOfWork.Users.GetAllAsync();
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);
            var refundCredits = (await _unitOfWork.AccountTransactions.GetAllAsync())
                .Where(t => t.ReferenceType == "ReturnRequest"
                    && t.ReferenceId.HasValue
                    && AccountTransactionTypeHelper.IsCredit(t.TransactionType))
                .GroupBy(t => t.ReferenceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedDate).First());

            var result = from r in returns
                         join a in accounts on r.AccountId equals a.AccountId into accGroup
                         from acc in accGroup.DefaultIfEmpty()
                         join o in orders on r.OrderId equals o.OrderId into orderGroup
                         from ord in orderGroup.DefaultIfEmpty()
                         join v in vehicles on r.VehicleId equals v.VehicleId into vehicleGroup
                         from veh in vehicleGroup.DefaultIfEmpty()
                         join u in users on r.ProcessedBy equals u.UserId into userGroup
                         from processedUser in userGroup.DefaultIfEmpty()
                         let displayStatus = VehicleStatusResolver.ResolveReturnDisplayStatus(r, veh)
                         select new ReturnRequestDto
                         {
                             ReturnRequestId = r.ReturnRequestId,
                             AccountId = r.AccountId,
                             AccountName = acc != null ? acc.AccountName : "Unknown",
                             OrderId = r.OrderId,
                             OrderNumber = ord != null ? ord.OrderNumber : $"Order #{r.OrderId}",
                             VehicleId = r.VehicleId,
                             VehicleChassisNumber = veh != null ? veh.ChassisNumber : "Unknown",
                             SubdealerUserId = veh?.SubdealerId ?? ord?.SubdealerId,
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
                             ModifiedDate = r.ModifiedDate
                         };

            if (request.ReturnRequestId.HasValue)
                result = result.Where(r => r.ReturnRequestId == request.ReturnRequestId.Value);

            if (request.AccountId.HasValue)
                result = result.Where(r => r.AccountId == request.AccountId.Value);

            if (request.SubdealerId.HasValue)
            {
                var subdealerAccountIds = accounts
                    .Where(a => a.SubdealerId == request.SubdealerId.Value)
                    .Select(a => a.AccountId)
                    .ToHashSet();
                result = result.Where(r =>
                    r.SubdealerUserId == request.SubdealerId.Value
                    || subdealerAccountIds.Contains(r.AccountId));
            }

            if (request.Status.HasValue)
                result = result.Where(r => r.Status == request.Status.Value);

            if (request.FromDate.HasValue)
                result = result.Where(r => r.CreatedDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                result = result.Where(r => r.CreatedDate <= request.ToDate.Value);

            return result.OrderByDescending(r => r.CreatedDate).ToList();
        }
    }
}

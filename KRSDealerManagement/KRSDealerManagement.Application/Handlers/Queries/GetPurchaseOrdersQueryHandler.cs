using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, IEnumerable<PurchaseOrderDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetPurchaseOrdersQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            var orders = await _unitOfWork.PurchaseOrders.GetAllAsync();
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var users = await _unitOfWork.Users.GetAllAsync();
            var allItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync()).ToList();
            var allVehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);

            var result = from o in orders
                         join a in accounts on o.AccountId equals a.AccountId into accGroup
                         from acc in accGroup.DefaultIfEmpty()
                         join u in users on o.SubdealerId equals u.UserId into userGroup
                         from user in userGroup.DefaultIfEmpty()
                         let orderVehicles = allVehicles.Where(v => v.PurchaseOrderId == o.OrderId).ToList()
                         let orderItems = allItems.Where(i => i.PurchaseOrderId == o.OrderId).ToList()
                         let displayStatus = VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, orderItems)
                         select new PurchaseOrderDto
                         {
                             OrderId = o.OrderId,
                             AccountId = o.AccountId,
                             AccountName = acc != null ? acc.AccountName : "Unknown",
                             SubdealerId = o.SubdealerId,
                             SubdealerName = user != null ? user.GetFullName() : "Unknown",
                             OrderNumber = o.OrderNumber,
                             TotalQuantity = o.TotalQuantity,
                             TotalAmount = o.TotalAmount,
                             Status = displayStatus,
                             StatusName = statusMap.TryGetValue(displayStatus, out var st) ? st.StatusName : null,
                             StatusBadgeClass = statusMap.TryGetValue(displayStatus, out st) ? st.BadgeClass : null,
                             CreatedByDealer = o.CreatedByDealer,
                             PendingItemCount = orderItems.Count(i => i.Status == 0),
                             ApprovedItemCount = orderItems.Count(i => i.Status == 1),
                             AdminNotes = o.AdminNotes,
                             SubdealerNotes = o.SubdealerNotes,
                             ApprovedBy = o.ApprovedBy,
                             ApprovedDate = o.ApprovedDate,
                             LastAllocatedDate = ResolveLastAllocatedDate(orderItems, o.ApprovedDate),
                             DeliveryDate = o.DeliveryDate,
                             CreatedDate = o.CreatedDate,
                             ModifiedDate = o.ModifiedDate
                         };

            if (request.SubdealerId.HasValue)
                result = result.Where(o => o.SubdealerId == request.SubdealerId.Value);

            if (request.DealershipId.HasValue)
            {
                var scopedUserIds = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                    .Where(a => a.IsActive && a.DealershipId == request.DealershipId.Value)
                    .Select(a => a.UserId)
                    .ToHashSet();
                result = result.Where(o => scopedUserIds.Contains(o.SubdealerId));
            }

            if (request.AccountId.HasValue)
                result = result.Where(o => o.AccountId == request.AccountId.Value);

            if (request.Status.HasValue)
                result = result.Where(o => o.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                result = result.Where(o => o.OrderNumber.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                result = result.Where(o => o.CreatedDate >= from);
            }

            if (request.ToDate.HasValue)
            {
                var toExclusive = request.ToDate.Value.Date.AddDays(1);
                result = result.Where(o => o.CreatedDate < toExclusive);
            }

            return result.OrderByDescending(o => o.CreatedDate).ToList();
        }

        private static DateTime? ResolveLastAllocatedDate(IEnumerable<Domain.Entities.PurchaseOrderItem> items, DateTime? orderApprovedDate)
        {
            var itemDates = items
                .Select(i => i.ApprovedDate ?? i.RejectedDate)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            if (itemDates.Count > 0)
                return itemDates.Max();

            return orderApprovedDate;
        }
    }
}

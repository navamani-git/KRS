using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Helpers;
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
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var userOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var allItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync()).ToList();
            var allVehicles = VehicleLifecycleHelper.FilterActiveLifecycle(await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);

            var result = from o in orders
                         join a in accounts on o.AccountId equals a.AccountId into accGroup
                         from acc in accGroup.DefaultIfEmpty()
                         let orderVehicles = allVehicles.Where(v => v.PurchaseOrderId == o.OrderId).ToList()
                         let orderItems = allItems.Where(i => i.PurchaseOrderId == o.OrderId).ToList()
                         let displayStatus = VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, orderItems)
                         select new PurchaseOrderDto
                         {
                             OrderId = o.OrderId,
                             AccountId = o.AccountId,
                             AccountName = acc != null ? acc.AccountName : "Unknown",
                             SubdealerId = o.SubdealerId,
                             SubdealerName = SubdealerOrgService.ResolveDisplayName(o.SubdealerId, userOrgRoles, orgs, users),
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
            {
                var orgId = await SubdealerOrgService.ResolveOrgIdAsync(_unitOfWork, request.SubdealerId.Value);
                result = result.Where(o => o.SubdealerId == orgId);
            }

            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
            if (dealershipFilter != null)
            {
                var orgRoles = await _unitOfWork.UserOrgRoles.GetAllAsync();
                var scopedOrgIds = DealershipQueryScope.GetScopedSubdealerOrgIds(orgRoles, dealershipFilter);
                result = result.Where(o => scopedOrgIds.Contains(o.SubdealerId));
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

            if (request.ColumnFilters is { Count: > 0 } cf)
            {
                result = result.Where(o =>
                    GridFilterHelper.MatchesContains(o.OrderNumber, GridFilterHelper.GetFilter(cf, "orderNumber"))
                    && GridFilterHelper.MatchesContains(o.SubdealerName, GridFilterHelper.GetFilter(cf, "subdealer"))
                    && GridFilterHelper.MatchesContains(o.GetStatusDisplay(), GridFilterHelper.GetFilter(cf, "status"))
                    && GridFilterHelper.MatchesContains(o.TotalAmount.ToString("N2"), GridFilterHelper.GetFilter(cf, "amount"))
                    && GridFilterHelper.MatchesContains(o.TotalQuantity.ToString(), GridFilterHelper.GetFilter(cf, "qty"))
                    && GridFilterHelper.MatchesContains(o.PendingItemCount.ToString(), GridFilterHelper.GetFilter(cf, "pending"))
                    && GridFilterHelper.MatchesContains(o.AdminNotes ?? o.SubdealerNotes, GridFilterHelper.GetFilter(cf, "notes"))
                    && GridFilterHelper.MatchesDate(o.CreatedDate, GridFilterHelper.GetDateFilter(cf, "created"), GridFilterHelper.GetDateFilter(cf, "created"))
                    && GridFilterHelper.MatchesDate(o.LastAllocatedDate, GridFilterHelper.GetDateFilter(cf, "allocated"), GridFilterHelper.GetDateFilter(cf, "allocated"))
                    && GridFilterHelper.MatchesDate(o.ApprovedDate, GridFilterHelper.GetDateFilter(cf, "approved"), GridFilterHelper.GetDateFilter(cf, "approved")));

                if (GridFilterHelper.TryGetSort(cf, out _, out _))
                    return GridFilterHelper.ApplySort(result, cf, OrderSortText, OrderSortDates).ToList();
            }

            return result.OrderByDescending(o => o.CreatedDate).ToList();
        }

        private static readonly Dictionary<string, Func<PurchaseOrderDto, string?>> OrderSortText = new(StringComparer.OrdinalIgnoreCase)
        {
            ["orderNumber"] = o => o.OrderNumber,
            ["subdealer"] = o => o.SubdealerName,
            ["qty"] = o => o.TotalQuantity.ToString(),
            ["pending"] = o => o.PendingItemCount.ToString(),
            ["amount"] = o => o.TotalAmount.ToString("N2"),
            ["status"] = o => o.GetStatusDisplay(),
            ["notes"] = o => o.AdminNotes ?? o.SubdealerNotes
        };

        private static readonly Dictionary<string, Func<PurchaseOrderDto, DateTime?>> OrderSortDates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["created"] = o => o.CreatedDate,
            ["allocated"] = o => o.LastAllocatedDate,
            ["approved"] = o => o.ApprovedDate
        };

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

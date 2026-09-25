using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Queries;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetPurchaseOrdersPageQueryHandler : IRequestHandler<GetPurchaseOrdersPageQuery, PurchaseOrderPageResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPurchaseOrdersPageQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<PurchaseOrderPageResult> Handle(GetPurchaseOrdersPageQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize > 0 ? request.PageSize : 50;
            var page = request.Page > 0 ? request.Page : 1;
            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
            if (dealershipFilter is { Count: 0 })
            {
                return new PurchaseOrderPageResult { Page = 1, PageSize = pageSize };
            }

            int? subdealerId = request.SubdealerId;
            if (subdealerId is > 0)
                subdealerId = await SubdealerOrgService.ResolveOrgIdAsync(_unitOfWork, subdealerId.Value);

            var filters = request.ColumnFilters;
            GridFilterHelper.TryGetSort(filters, out var sortColumn, out var sortDesc);
            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                sortColumn = "created";
                sortDesc = true;
            }

            var criteria = new PurchaseOrderListCriteria
            {
                OrderId = request.OrderId,
                SubdealerId = subdealerId,
                AccountId = request.AccountId,
                Status = request.Status,
                DealershipIds = dealershipFilter?.ToArray(),
                ApplyDealershipFilter = dealershipFilter != null,
                FromDate = request.OrderId.HasValue ? null : request.FromDate?.Date,
                ToDateExclusive = request.OrderId.HasValue ? null : request.ToDate?.Date.AddDays(1),
                SearchTerm = request.SearchTerm,
                OrderNumber = GridFilterHelper.GetFilter(filters, "orderNumber"),
                Subdealer = GridFilterHelper.GetFilter(filters, "subdealer"),
                Created = GridFilterHelper.GetDateFilter(filters, "created"),
                Allocated = GridFilterHelper.GetDateFilter(filters, "allocated"),
                Qty = GridFilterHelper.GetFilter(filters, "qty"),
                Pending = GridFilterHelper.GetFilter(filters, "pending"),
                Amount = GridFilterHelper.GetFilter(filters, "amount"),
                StatusText = GridFilterHelper.GetFilter(filters, "status"),
                Notes = GridFilterHelper.GetFilter(filters, "notes"),
                SortColumn = sortColumn,
                SortDescending = sortDesc,
                Skip = request.IncludeAll ? 0 : (page - 1) * pageSize,
                Take = pageSize,
                IncludeAll = request.IncludeAll
            };

            var pageResult = await _unitOfWork.PurchaseOrders.QueryPageAsync(criteria);
            if (!request.IncludeAll && pageResult.Items.Count == 0 && pageResult.TotalItems > 0 && criteria.Skip > 0)
            {
                var lastPage = Math.Max(1, (int)Math.Ceiling(pageResult.TotalItems / (double)pageSize));
                criteria.Skip = (lastPage - 1) * pageSize;
                page = lastPage;
                pageResult = await _unitOfWork.PurchaseOrders.QueryPageAsync(criteria);
            }

            return new PurchaseOrderPageResult
            {
                Items = pageResult.Items.Select(Map).ToList(),
                TotalItems = pageResult.TotalItems,
                PendingCount = pageResult.PendingCount,
                Page = request.IncludeAll ? 1 : page,
                PageSize = request.IncludeAll ? Math.Max(pageResult.TotalItems, 1) : pageSize
            };
        }

        private static PurchaseOrderDto Map(PurchaseOrderListRow row) => new()
        {
            OrderId = row.OrderId,
            AccountId = row.AccountId,
            AccountName = row.AccountName,
            SubdealerId = row.SubdealerId,
            SubdealerName = row.SubdealerName,
            OrderNumber = row.OrderNumber,
            TotalQuantity = row.TotalQuantity,
            TotalAmount = row.TotalAmount,
            Status = row.Status,
            StatusName = row.StatusName,
            StatusBadgeClass = row.StatusBadgeClass,
            CreatedByDealer = row.CreatedByDealer,
            PendingItemCount = row.PendingItemCount,
            ApprovedItemCount = row.ApprovedItemCount,
            AdminNotes = row.AdminNotes,
            SubdealerNotes = row.SubdealerNotes,
            ApprovedBy = row.ApprovedBy,
            ApprovedDate = row.ApprovedDate,
            LastAllocatedDate = row.LastAllocatedDate,
            CreatedDate = row.CreatedDate,
            ModifiedDate = row.ModifiedDate ?? row.CreatedDate
        };
    }
}

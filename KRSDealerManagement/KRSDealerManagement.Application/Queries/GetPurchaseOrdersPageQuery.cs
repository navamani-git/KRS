using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetPurchaseOrdersPageQuery : IRequest<PurchaseOrderPageResult>
    {
        public int? OrderId { get; set; }
        public int? SubdealerId { get; set; }
        public int? AccountId { get; set; }
        public int? Status { get; set; }
        public int? DealershipId { get; set; }
        public List<int>? DealershipIds { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public Dictionary<string, string>? ColumnFilters { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool IncludeAll { get; set; }
    }

    public class PurchaseOrderPageResult
    {
        public List<PurchaseOrderDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int PendingCount { get; set; }
        public int TotalPages => PageSize <= 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    }
}

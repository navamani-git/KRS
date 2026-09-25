namespace KRSDealerManagement.Domain.Queries
{
    public class PurchaseOrderListCriteria
    {
        public int? OrderId { get; set; }
        public int? SubdealerId { get; set; }
        public int? AccountId { get; set; }
        public int? Status { get; set; }
        public IReadOnlyCollection<int>? DealershipIds { get; set; }
        public bool ApplyDealershipFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDateExclusive { get; set; }
        public string? SearchTerm { get; set; }
        public string? OrderNumber { get; set; }
        public string? Subdealer { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Allocated { get; set; }
        public string? Qty { get; set; }
        public string? Pending { get; set; }
        public string? Amount { get; set; }
        public string? StatusText { get; set; }
        public string? Notes { get; set; }
        public string SortColumn { get; set; } = "created";
        public bool SortDescending { get; set; } = true;
        public int Skip { get; set; }
        public int Take { get; set; } = 50;
        /// <summary>Export: return every row that matches the filter, without a page size.</summary>
        public bool IncludeAll { get; set; }
    }

    public class PurchaseOrderListRow
    {
        public int OrderId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public int SubdealerId { get; set; }
        public string SubdealerName { get; set; } = "";
        public string OrderNumber { get; set; } = "";
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? StatusBadgeClass { get; set; }
        public bool CreatedByDealer { get; set; }
        public int PendingItemCount { get; set; }
        public int ApprovedItemCount { get; set; }
        public string? AdminNotes { get; set; }
        public string? SubdealerNotes { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? LastAllocatedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class PurchaseOrderListPage
    {
        public int TotalItems { get; set; }
        public int PendingCount { get; set; }
        public List<PurchaseOrderListRow> Items { get; set; } = new();
    }
}

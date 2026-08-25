using MediatR;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get dashboard summary statistics
    /// </summary>
    public class GetDashboardSummaryQuery : IRequest<DashboardSummary>
    {
        public int? SubdealerId { get; set; } // Optional: for subdealer dashboard
        /// <summary>When set, staff dashboard counts are limited to this dealership's subdealers.</summary>
        public int? DealershipId { get; set; }
        public bool IncludeRecentActivities { get; set; } // System admin only
        /// <summary>When false, skip loading payment pending count (e.g. branch manager).</summary>
        public bool IncludePaymentPending { get; set; } = true;
    }

    /// <summary>
    /// Dashboard summary data
    /// </summary>
    public class DashboardSummary
    {
        public int TotalSubdealers { get; set; }
        public int TotalAccounts { get; set; }
        public decimal TotalBalance { get; set; }
        public int PendingPurchaseOrders { get; set; }
        public int PendingCommissions { get; set; }
        public int PendingReturnRequests { get; set; }
        public int PendingPayments { get; set; }
        public decimal TotalReservedAmount { get; set; }
        public int BookedToCustomerCount { get; set; }
        public int PaperReceivedCount { get; set; }
        public int InvoicedCount { get; set; }
        public int InsuranceCreatedCount { get; set; }
        public int RtoRequestedCount { get; set; }
        public int RegisteredCount { get; set; }
        public int DeliveredCount { get; set; }
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
    }

    /// <summary>
    /// Recent activity for dashboard
    /// </summary>
    public class RecentActivityItem
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserName { get; set; }
    }
}

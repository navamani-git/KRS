using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get purchase orders with advanced filtering
    /// </summary>
    public class GetPurchaseOrdersQuery : IRequest<IEnumerable<PurchaseOrderDto>>
    {
        public int? SubdealerId { get; set; } // Filter by subdealer
        public int? AccountId { get; set; } // Filter by account
        public int? Status { get; set; } // Filter by status (0=Pending, 1=Approved, etc.)
        public int? DealershipId { get; set; } // Filter by location via UserOrgRoles
        public DateTime? FromDate { get; set; } // Date range
        public DateTime? ToDate { get; set; }
        public string SearchTerm { get; set; } // Search by order number
    }
}

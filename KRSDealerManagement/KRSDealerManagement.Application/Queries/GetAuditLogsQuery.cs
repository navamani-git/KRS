using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get audit logs with comprehensive filtering
    /// WHO changed WHAT, WHEN, and WHY
    /// </summary>
    public class GetAuditLogsQuery : IRequest<IEnumerable<AuditLogDto>>
    {
        public string EntityType { get; set; } // Filter by entity type
        public int? EntityId { get; set; }
        public string Action { get; set; } // Create, Update, Approve, Reject, etc.
        public int? UserId { get; set; }
        public string UserRole { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SearchTerm { get; set; } // Search by remarks or entity name
    }
}

using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Services
{
    /// <summary>
    /// Service for automatically logging operations to AuditLog and AccountTransaction tables
    /// Called by every handler to ensure 100% audit trail coverage
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log an action (Create, Update, Delete, Approve, Reject, etc.)
        /// WHO: userId, userRole
        /// WHAT: entityType, entityId, oldValue, newValue
        /// WHEN: automatic DateTime.UtcNow
        /// WHY: remarks
        /// </summary>
        Task LogActionAsync(
            string entityType,
            int entityId,
            string action,
            int userId,
            string userRole,
            string newValue,
            string oldValue = null,
            string remarks = null);

        /// <summary>
        /// Log a balance transaction (Debit or Credit)
        /// Automatically called when balance changes
        /// </summary>
        Task LogTransactionAsync(
            int accountId,
            int transactionType, // 1=Debit, 2=Credit
            decimal amount,
            decimal balanceAfter,
            string reason,
            int? referenceId = null,
            string referenceType = null,
            string remarks = null,
            int initiatedBy = 0);

        /// <summary>
        /// Get audit logs with filtering
        /// Used by GetAuditLogsQueryHandler
        /// </summary>
        Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(
            string entityType = null,
            int? entityId = null,
            string action = null,
            int? userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Get account transaction history
        /// Used by GetAccountTransactionsQueryHandler
        /// </summary>
        Task<IEnumerable<AccountTransactionDto>> GetAccountTransactionsAsync(
            int accountId,
            int? transactionType = null,
            string referenceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
    }
}

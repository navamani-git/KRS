using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Application.DTOs;
using AutoMapper;

namespace KRSDealerManagement.Application.Services
{
    /// <summary>
    /// Implementation of IAuditService
    /// Automatically logs all operations to AuditLog and AccountTransaction tables
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AuditService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task LogActionAsync(
            string entityType, int entityId, string action, int userId, string userRole,
            string newValue, string oldValue = null, string remarks = null)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                UserRole = userRole,
                NewValue = newValue,
                OldValue = oldValue,
                Remarks = remarks,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task LogTransactionAsync(
            int accountId, int transactionType, decimal amount, decimal balanceAfter,
            string reason, int? referenceId = null, string referenceType = null,
            string remarks = null, int initiatedBy = 0)
        {
            var transaction = new AccountTransaction
            {
                AccountId = accountId,
                TransactionType = transactionType, // 1 = Debit, 2 = Credit, 3 = Reserved, 4 = Released
                Amount = amount,
                BalanceAfterTransaction = balanceAfter,
                Reason = reason,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Remarks = remarks,
                InitiatedBy = initiatedBy,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.AccountTransactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(
            string entityType = null, int? entityId = null, string action = null,
            int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var allLogs = await _unitOfWork.AuditLogs.GetAllAsync();
            var query = allLogs.AsEnumerable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(x => x.EntityType == entityType);

            if (entityId.HasValue)
                query = query.Where(x => x.EntityId == entityId.Value);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action == action);

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedDate <= toDate.Value);

            var result = query.OrderByDescending(x => x.CreatedDate).ToList();
            return _mapper.Map<IEnumerable<AuditLogDto>>(result);
        }

        public async Task<IEnumerable<AccountTransactionDto>> GetAccountTransactionsAsync(
            int accountId, int? transactionType = null, string referenceType = null,
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Get all transactions and filter
            var allTransactions = await _unitOfWork.AccountTransactions.GetAllAsync();
            var query = allTransactions.Where(x => x.AccountId == accountId);

            if (transactionType.HasValue)
                query = query.Where(x => x.TransactionType == transactionType.Value);

            if (!string.IsNullOrEmpty(referenceType))
                query = query.Where(x => x.ReferenceType == referenceType);

            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedDate <= toDate.Value);

            var result = query.OrderByDescending(x => x.CreatedDate).ToList();
            return _mapper.Map<IEnumerable<AccountTransactionDto>>(result);
        }
    }
}

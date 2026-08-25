using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AdminDeleteAccountTransactionCommandHandler : IRequestHandler<AdminDeleteAccountTransactionCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AdminDeleteAccountTransactionCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AdminDeleteAccountTransactionCommand request, CancellationToken cancellationToken)
        {
            var transaction = await _unitOfWork.AccountTransactions.GetByIdAsync(request.TransactionId);
            if (transaction == null || transaction.IsDeleted)
                return false;

            var linkedBefore = await AccountTransactionCascadeHelper.LoadLinkedSnapshotAsync(_unitOfWork, transaction);
            var oldSnapshot = AccountTransactionSnapshotHelper.Serialize(transaction, linkedBefore);

            await AccountTransactionCascadeHelper.ApplyDeleteCascadeAsync(_unitOfWork, transaction);

            transaction.IsDeleted = true;
            await _unitOfWork.AccountTransactions.UpdateAsync(transaction);
            await AccountTransactionBalanceRecalcHelper.RecalculateAccountAsync(_unitOfWork, transaction.AccountId);

            await _unitOfWork.AccountTransactionCorrections.AddAsync(new AccountTransactionCorrection
            {
                TransactionId = transaction.TransactionId,
                AccountId = transaction.AccountId,
                Action = "Delete",
                OldSnapshot = oldSnapshot,
                NewSnapshot = null,
                CorrectionReason = request.DeleteReason.Trim(),
                CorrectedBy = request.DeletedBy,
                CorrectedByName = request.DeletedByName,
                CreatedDate = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "AccountTransaction",
                entityId: transaction.TransactionId,
                action: "AdminDelete",
                userId: request.DeletedBy,
                userRole: "Admin",
                newValue: "Deleted",
                oldValue: oldSnapshot,
                remarks: request.DeleteReason.Trim());

            return true;
        }
    }
}

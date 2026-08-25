using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AdminEditAccountTransactionCommandHandler : IRequestHandler<AdminEditAccountTransactionCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AdminEditAccountTransactionCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AdminEditAccountTransactionCommand request, CancellationToken cancellationToken)
        {
            var transaction = await _unitOfWork.AccountTransactions.GetByIdAsync(request.TransactionId);
            if (transaction == null || transaction.IsDeleted)
                return false;

            var linkedBefore = await AccountTransactionCascadeHelper.LoadLinkedSnapshotAsync(_unitOfWork, transaction);
            var oldSnapshot = AccountTransactionSnapshotHelper.Serialize(transaction, linkedBefore);

            transaction.TransactionType = request.TransactionType;
            transaction.Amount = request.Amount;
            transaction.CreatedDate = request.TransactionDate;
            transaction.Reason = request.Reason.Trim();
            transaction.Remarks = request.Remarks?.Trim();

            await AccountTransactionCascadeHelper.ApplyEditCascadeAsync(
                _unitOfWork,
                transaction,
                request.RequestedAmount,
                request.ApprovedPaymentAmount,
                request.PaymentSubmittedDate,
                request.PaymentApprovedDate,
                request.PaymentReceivedDate,
                request.CustomerName,
                request.PaymentTypeId,
                request.FinanceNameId,
                request.VinNumber,
                request.CommissionAmount);

            await _unitOfWork.AccountTransactions.UpdateAsync(transaction);
            await AccountTransactionBalanceRecalcHelper.RecalculateAccountAsync(_unitOfWork, transaction.AccountId);

            var linkedAfter = await AccountTransactionCascadeHelper.LoadLinkedSnapshotAsync(_unitOfWork, transaction);
            var newSnapshot = AccountTransactionSnapshotHelper.Serialize(transaction, linkedAfter);

            await _unitOfWork.AccountTransactionCorrections.AddAsync(new AccountTransactionCorrection
            {
                TransactionId = transaction.TransactionId,
                AccountId = transaction.AccountId,
                Action = "Edit",
                OldSnapshot = oldSnapshot,
                NewSnapshot = newSnapshot,
                CorrectionReason = request.CorrectionReason.Trim(),
                CorrectedBy = request.CorrectedBy,
                CorrectedByName = request.CorrectedByName,
                CreatedDate = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "AccountTransaction",
                entityId: transaction.TransactionId,
                action: "AdminEdit",
                userId: request.CorrectedBy,
                userRole: "Admin",
                newValue: newSnapshot,
                oldValue: oldSnapshot,
                remarks: request.CorrectionReason.Trim());

            return true;
        }
    }
}

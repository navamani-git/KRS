using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AdjustSubdealerAccountCommandHandler : IRequestHandler<AdjustSubdealerAccountCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AdjustSubdealerAccountCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AdjustSubdealerAccountCommand request, CancellationToken cancellationToken)
        {
            var type = request.AdjustmentType.Trim();
            var isCredit = type.Equals("Credit", StringComparison.OrdinalIgnoreCase);
            var isDebit = type.Equals("Debit", StringComparison.OrdinalIgnoreCase);
            if (!isCredit && !isDebit)
                throw new InvalidOperationException("Adjustment type must be Credit or Debit.");

            if (request.Amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");

            var description = request.Description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException("Description is required.");

            var account = await SubdealerOrgService.GetPermissionAccountAsync(_unitOfWork, request.SubdealerId)
                ?? throw new InvalidOperationException("Subdealer account not found.");

            var balance = await _unitOfWork.AccountBalances.GetByIdAsync(account.AccountId)
                ?? (await _unitOfWork.AccountBalances.GetAllAsync())
                    .FirstOrDefault(b => b.SubdealerId == request.SubdealerId)
                ?? throw new InvalidOperationException("Account balance not found.");

            if (isCredit)
                balance.CurrentBalance += request.Amount;
            else
                balance.CurrentBalance -= request.Amount;

            balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            balance.LastTransactionDate = DateTime.UtcNow;
            balance.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.AccountBalances.UpdateAsync(balance);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogTransactionAsync(
                accountId: account.AccountId,
                transactionType: isCredit ? 2 : 1,
                amount: request.Amount,
                balanceAfter: balance.CurrentBalance,
                reason: description,
                referenceType: "ManualAdjustment",
                referenceId: null,
                remarks: request.Remarks?.Trim(),
                initiatedBy: request.AdjustedBy);

            await _auditService.LogActionAsync(
                entityType: "AccountBalance",
                entityId: balance.BalanceId,
                action: isCredit ? "ManualCredit" : "ManualDebit",
                userId: request.AdjustedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    request.SubdealerId,
                    Type = type,
                    request.Amount,
                    description,
                    balance.CurrentBalance
                }),
                remarks: request.Remarks);

            return true;
        }
    }
}

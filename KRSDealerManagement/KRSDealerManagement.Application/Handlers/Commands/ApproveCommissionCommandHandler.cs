using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Shared.Helpers;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class ApproveCommissionCommandHandler : IRequestHandler<ApproveCommissionCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IAuditService _auditService;

        public ApproveCommissionCommandHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _auditService = auditService;
        }

        public async Task<bool> Handle(ApproveCommissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var commission = await _unitOfWork.Commissions.GetByIdAsync(request.CommissionId);
                if (commission == null || !commission.CanBeApproved())
                    return false;

                var accounts = await _mediator.Send(new GetSubdealerAccountsQuery
                {
                    SubdealerId = commission.SubdealerId,
                    IsActive = true
                }, cancellationToken);

                var account = accounts.FirstOrDefault(a =>
                    string.Equals(a.AccountType, "Main", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.AccountName, "Main Account", StringComparison.OrdinalIgnoreCase))
                    ?? accounts.FirstOrDefault();

                if (account == null)
                    throw new InvalidOperationException("No active account found for the subdealer.");

                commission.Approve(request.ApprovedBy);
                commission.MarkAsPaid();
                commission.ApprovedAmount = commission.CommissionAmount;
                if (!string.IsNullOrWhiteSpace(request.Remarks))
                {
                    var note = $"[Approved] {request.Remarks.Trim()}";
                    commission.Notes = string.IsNullOrWhiteSpace(commission.Notes)
                        ? note
                        : $"{commission.Notes} {note}";
                }

                var balance = (await _unitOfWork.AccountBalances.GetAllAsync())
                    .FirstOrDefault(b => b.SubdealerAccountId == account.AccountId)
                    ?? (await _unitOfWork.AccountBalances.GetAllAsync())
                        .FirstOrDefault(b => b.SubdealerId == commission.SubdealerId);

                if (balance == null)
                    throw new InvalidOperationException("Account balance record not found.");

                balance.CurrentBalance += commission.CommissionAmount;
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                balance.LastTransactionDate = DateTime.UtcNow;
                balance.ModifiedDate = DateTime.UtcNow;

                await _unitOfWork.AccountBalances.UpdateAsync(balance);
                await _unitOfWork.Commissions.UpdateAsync(commission);
                await _unitOfWork.SaveChangesAsync();

                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(commission.VehicleId);
                var chassis = TransactionReasonHelper.FormatChassis(vehicle?.ChassisNumber);

                await _auditService.LogTransactionAsync(
                    accountId: account.AccountId,
                    transactionType: (int)TransactionTypeEnum.CommissionApproved,
                    amount: commission.CommissionAmount,
                    balanceAfter: balance.CurrentBalance,
                    reason: TransactionReasonHelper.Commission(chassis),
                    referenceType: "Commission",
                    referenceId: commission.CommissionId,
                    remarks: request.Remarks,
                    initiatedBy: request.ApprovedBy);

                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "Commission",
                    entityId: commission.CommissionId,
                    action: "Approve",
                    userId: request.ApprovedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        commission.CommissionId,
                        commission.SubdealerId,
                        commission.VehicleId,
                        ChassisNumber = chassis,
                        commission.CommissionAmount,
                        AccountId = account.AccountId,
                        BalanceAfter = balance.CurrentBalance,
                        request.Remarks
                    }));

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

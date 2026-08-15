using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Creates a payment submission from subdealer
    /// </summary>
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreatePaymentCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            var payment = new Payment
            {
                AccountId = request.AccountId,
                SubdealerId = request.SubdealerId,
                Amount = request.Amount,
                PaymentType = request.PaymentType,
                PaymentTypeId = request.PaymentTypeId,
                PaymentDate = request.PaymentDate,
                Status = 0, // Pending
                SubdealerRemarks = request.SubdealerRemarks,
                CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim().ToUpperInvariant(),
                FinanceNameId = request.FinanceNameId,
                VinNumber = string.IsNullOrWhiteSpace(request.VinNumber) ? null : request.VinNumber.Trim().ToUpperInvariant(),
                PaymentProofPath = request.PaymentProofPath,
                PaymentProof2Path = request.PaymentProof2Path,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var paymentId = await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "Payment",
                entityId: paymentId,
                action: "Create",
                userId: request.CreatedBy,
                userRole: "Subdealer",
                newValue: JsonSerializer.Serialize(new
                {
                    Amount = request.Amount,
                    Type = request.PaymentType,
                    Date = request.PaymentDate
                })
            );

            return paymentId;
        }
    }

    /// <summary>
    /// Approves payment and credits account balance
    /// </summary>
    public class ApprovePaymentCommandHandler : IRequestHandler<ApprovePaymentCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public ApprovePaymentCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(ApprovePaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId);
                if (payment == null || !payment.CanBeApproved()) return false;

                payment.Approve(request.ApprovedBy, request.Remarks);

                // Always credit on approval (ApplyToBalance defaults true; keep as safety flag)
                var shouldApply = request.ApplyToBalance;
                if (shouldApply)
                {
                    var balance = await GetOrCreateBalanceAsync(payment);
                    balance.CurrentBalance += payment.Amount;
                    balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                    balance.LastTransactionDate = DateTime.UtcNow;
                    balance.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.AccountBalances.UpdateAsync(balance);

                    payment.IsApplied = true;

                    await _auditService.LogTransactionAsync(
                        accountId: payment.AccountId,
                        transactionType: 2, // Credit
                        amount: payment.Amount,
                        balanceAfter: balance.CurrentBalance,
                        reason: $"Payment #{payment.PaymentId} approved and credited",
                        referenceType: "Payment",
                        referenceId: payment.PaymentId,
                        remarks: request.Remarks,
                        initiatedBy: request.ApprovedBy
                    );
                }

                await _unitOfWork.Payments.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "Payment",
                    entityId: payment.PaymentId,
                    action: "Approve",
                    userId: request.ApprovedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        PaymentId = request.PaymentId,
                        ApplyToBalance = shouldApply,
                        IsApplied = payment.IsApplied,
                        Remarks = request.Remarks
                    })
                );

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error approving payment: {ex.Message}", ex);
            }
        }

        private async Task<AccountBalance> GetOrCreateBalanceAsync(Payment payment)
        {
            var balances = (await _unitOfWork.AccountBalances.GetAllAsync()).ToList();
            var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == payment.AccountId)
                       ?? balances.FirstOrDefault(b => b.SubdealerId == payment.SubdealerId);

            if (balance != null)
                return balance;

            // Ensure account exists
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var account = accounts.FirstOrDefault(a => a.AccountId == payment.AccountId)
                       ?? accounts.FirstOrDefault(a => a.SubdealerId == payment.SubdealerId);

            if (account == null)
            {
                account = new SubdealerAccount
                {
                    SubdealerId = payment.SubdealerId,
                    AccountName = "Main Account",
                    AccountType = "Main",
                    Description = "Auto-created on payment approval",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                account.AccountId = await _unitOfWork.SubdealerAccounts.AddAsync(account);
                payment.AccountId = account.AccountId;
            }

            balance = new AccountBalance
            {
                SubdealerAccountId = account.AccountId,
                SubdealerId = payment.SubdealerId,
                CurrentBalance = 0,
                ReservedAmount = 0,
                AvailableBalance = 0,
                InitialBalance = 0,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var balanceId = await _unitOfWork.AccountBalances.AddAsync(balance);
            balance.BalanceId = balanceId;
            return balance;
        }
    }

    /// <summary>
    /// Rejects a payment submission
    /// </summary>
    public class RejectPaymentCommandHandler : IRequestHandler<RejectPaymentCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public RejectPaymentCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(RejectPaymentCommand request, CancellationToken cancellationToken)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId);
            if (payment == null || !payment.CanBeRejected()) return false;

            payment.Reject(request.RejectedBy, request.Remarks);
            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "Payment",
                entityId: payment.PaymentId,
                action: "Reject",
                userId: request.RejectedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new { Remarks = request.Remarks })
            );

            return true;
        }
    }
}

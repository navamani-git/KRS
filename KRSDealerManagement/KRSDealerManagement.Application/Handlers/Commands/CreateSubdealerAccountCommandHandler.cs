using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Creates an additional account for an existing subdealer
    /// </summary>
    public class CreateSubdealerAccountCommandHandler : IRequestHandler<CreateSubdealerAccountCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateSubdealerAccountCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateSubdealerAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Create account
                var account = new SubdealerAccount
                {
                    SubdealerId = request.SubdealerId,
                    AccountName = request.AccountName,
                    AccountType = request.AccountType,
                    Description = request.Description,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                var accountId = await _unitOfWork.SubdealerAccounts.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();

                // Create balance
                var balance = new AccountBalance
                {
                    SubdealerAccountId = accountId,
                    SubdealerId = request.SubdealerId,
                    CurrentBalance = request.InitialBalance,
                    ReservedAmount = 0,
                    AvailableBalance = request.InitialBalance,
                    InitialBalance = request.InitialBalance,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                await _unitOfWork.AccountBalances.AddAsync(balance);

                if (request.InitialBalance > 0)
                {
                    await _auditService.LogTransactionAsync(
                        accountId: accountId,
                        transactionType: 2,
                        amount: request.InitialBalance,
                        balanceAfter: request.InitialBalance,
                        reason: "Initial balance on account creation",
                        referenceType: "AccountCreation",
                        referenceId: accountId,
                        initiatedBy: request.CreatedBy
                    );
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "SubdealerAccount",
                    entityId: accountId,
                    action: "Create",
                    userId: request.CreatedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        SubdealerId = request.SubdealerId,
                        AccountName = request.AccountName,
                        AccountType = request.AccountType,
                        InitialBalance = request.InitialBalance
                    })
                );

                return accountId;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error creating account: {ex.Message}", ex);
            }
        }
    }
}

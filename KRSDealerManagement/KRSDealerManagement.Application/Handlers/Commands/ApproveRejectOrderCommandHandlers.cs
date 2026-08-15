using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Approves a purchase order - deducts amount from balance
    /// </summary>
    public class ApprovePurchaseOrderItemCommandHandler : IRequestHandler<ApprovePurchaseOrderItemCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public ApprovePurchaseOrderItemCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(ApprovePurchaseOrderItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Get order
                var order = await _unitOfWork.PurchaseOrders.GetByIdAsync(request.OrderId);
                if (order == null) return false;

                // Get balance
                var balances = await _unitOfWork.AccountBalances.GetAllAsync();
                var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == order.AccountId);
                if (balance == null) return false;

                // Approve order
                order.Approve(request.ApprovedBy);
                order.AdminNotes = request.Remarks;
                await _unitOfWork.PurchaseOrders.UpdateAsync(order);

                // Debit from current balance and release reserved
                balance.CurrentBalance -= request.Amount;
                balance.ReservedAmount = Math.Max(0, balance.ReservedAmount - request.Amount);
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                balance.LastTransactionDate = DateTime.UtcNow;
                balance.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.AccountBalances.UpdateAsync(balance);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Log debit transaction
                await _auditService.LogTransactionAsync(
                    accountId: order.AccountId,
                    transactionType: 1, // Debit
                    amount: request.Amount,
                    balanceAfter: balance.CurrentBalance,
                    reason: $"Order {order.OrderNumber} approved",
                    referenceType: "PurchaseOrder",
                    referenceId: order.OrderId,
                    remarks: request.Remarks,
                    initiatedBy: request.ApprovedBy
                );

                await _auditService.LogActionAsync(
                    entityType: "PurchaseOrder",
                    entityId: order.OrderId,
                    action: "Approve",
                    userId: request.ApprovedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new { OrderId = request.OrderId, Amount = request.Amount, Remarks = request.Remarks })
                );

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error approving order: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Rejects a purchase order - releases reserved balance
    /// </summary>
    public class RejectPurchaseOrderItemCommandHandler : IRequestHandler<RejectPurchaseOrderItemCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public RejectPurchaseOrderItemCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(RejectPurchaseOrderItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var order = await _unitOfWork.PurchaseOrders.GetByIdAsync(request.OrderId);
                if (order == null) return false;

                var balances = await _unitOfWork.AccountBalances.GetAllAsync();
                var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == order.AccountId);

                // Reject order
                order.Reject();
                order.AdminNotes = request.Remarks;
                await _unitOfWork.PurchaseOrders.UpdateAsync(order);

                // Release reserved amount
                if (balance != null)
                {
                    balance.ReservedAmount = Math.Max(0, balance.ReservedAmount - request.Amount);
                    balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                    balance.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.AccountBalances.UpdateAsync(balance);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogTransactionAsync(
                    accountId: order.AccountId,
                    transactionType: 4, // Released
                    amount: request.Amount,
                    balanceAfter: balance?.CurrentBalance ?? 0,
                    reason: $"Order {order.OrderNumber} rejected - amount released",
                    referenceType: "PurchaseOrder",
                    referenceId: order.OrderId,
                    remarks: request.Remarks,
                    initiatedBy: request.RejectedBy
                );

                await _auditService.LogActionAsync(
                    entityType: "PurchaseOrder",
                    entityId: order.OrderId,
                    action: "Reject",
                    userId: request.RejectedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new { OrderId = request.OrderId, Remarks = request.Remarks })
                );

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error rejecting order: {ex.Message}", ex);
            }
        }
    }
}

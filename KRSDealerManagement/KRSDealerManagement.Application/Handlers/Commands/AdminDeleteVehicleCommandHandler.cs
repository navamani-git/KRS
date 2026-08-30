using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Shared.Helpers;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AdminDeleteVehicleCommandHandler : IRequestHandler<AdminDeleteVehicleCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IAuditService _auditService;

        public AdminDeleteVehicleCommandHandler(IUnitOfWork unitOfWork, IMediator mediator, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AdminDeleteVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null) return false;

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
            if (booking != null)
                throw new InvalidOperationException(
                    "Cannot delete a vehicle that has a customer booking. Cancel or reassign the booking first.");

            var hasCommission = (await _unitOfWork.Commissions.GetAllAsync())
                .Any(c => c.VehicleId == vehicle.VehicleId && c.Status != (int)CommissionStatusEnum.Rejected);
            if (hasCommission)
                throw new InvalidOperationException(
                    "Cannot delete a vehicle with submitted or approved commission records.");

            var hasReturn = (await _unitOfWork.ReturnRequests.GetAllAsync())
                .Any(r => r.VehicleId == vehicle.VehicleId);
            if (hasReturn)
                throw new InvalidOperationException(
                    "Cannot delete a vehicle with return request history. Resolve returns first.");

            if (UnifiedVehicleStatus.IsBookingPhase(vehicle.Status))
                throw new InvalidOperationException(
                    "Cannot delete a vehicle in customer booking lifecycle status.");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                if (vehicle.SubdealerId.HasValue && vehicle.Status == UnifiedVehicleStatus.ApprovedByDealer)
                {
                    await CreditSubdealerRefundAsync(
                        vehicle.SubdealerId.Value,
                        vehicle.CurrentPrice,
                        vehicle.VehicleId,
                        vehicle.ChassisNumber,
                        request.DeletedBy,
                        request.DeleteReason);
                }

                await UnlinkPurchaseOrderItemsAsync(vehicle.VehicleId);

                if (vehicle.VehicleMasterId > 0)
                    await VehicleAllocationHelper.ReleaseMasterAsync(
                        _unitOfWork, vehicle.VehicleMasterId, request.DeletedBy, request.DeleteReason);

                await VehicleAllocationHelper.LogSubdealerEventAsync(
                    _unitOfWork, vehicle.VehicleId, "Deleted", request.DeletedBy, request.DeleteReason);

                var deleted = await _unitOfWork.Vehicles.DeleteAsync(vehicle.VehicleId);
                if (!deleted)
                    throw new InvalidOperationException("Failed to delete vehicle record.");

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "Vehicle",
                    entityId: request.VehicleId,
                    action: "AdminDelete",
                    userId: request.DeletedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new { Deleted = true, request.DeleteReason }),
                    oldValue: JsonSerializer.Serialize(new
                    {
                        vehicle.ChassisNumber,
                        vehicle.ModelId,
                        vehicle.ColorId,
                        vehicle.Status,
                        vehicle.CurrentPrice,
                        vehicle.SubdealerId,
                        vehicle.PurchaseOrderId
                    }),
                    remarks: CorrectionNoteHelper.FormatEntry(request.DeletedByName, request.DeleteReason,
                        new[] { $"Vehicle #{vehicle.VehicleId} deleted" }));

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error deleting vehicle: {ex.Message}", ex);
            }
        }

        private async Task UnlinkPurchaseOrderItemsAsync(int vehicleId)
        {
            var items = (await _unitOfWork.PurchaseOrderItems.GetAllAsync())
                .Where(i => i.VehicleId == vehicleId)
                .ToList();

            foreach (var item in items)
            {
                item.VehicleId = null;
                item.ChassisNumber = null;
                item.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);
            }
        }

        private async Task CreditSubdealerRefundAsync(
            int subdealerId,
            decimal amount,
            int vehicleId,
            string chassis,
            int deletedBy,
            string reason)
        {
            if (amount <= 0) return;

            var orgId = await SubdealerOrgService.GetOrgIdForUserAsync(_unitOfWork, subdealerId);
            var walletUserId = orgId.HasValue
                ? await SubdealerOrgService.GetPrimaryUserIdForOrgAsync(_unitOfWork, orgId.Value) ?? subdealerId
                : subdealerId;

            var accounts = await _mediator.Send(new GetSubdealerAccountsQuery
            {
                SubdealerId = walletUserId,
                IsActive = true
            });

            var account = accounts.FirstOrDefault(a =>
                    string.Equals(a.AccountType, "Main", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.AccountName, "Main Account", StringComparison.OrdinalIgnoreCase))
                ?? accounts.FirstOrDefault()
                ?? throw new InvalidOperationException("Subdealer wallet account not found for refund.");

            var balance = (await _unitOfWork.AccountBalances.GetAllAsync())
                .FirstOrDefault(b => b.SubdealerAccountId == account.AccountId)
                ?? throw new InvalidOperationException("Subdealer wallet balance not found for refund.");

            balance.CurrentBalance += amount;
            balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            balance.LastTransactionDate = DateTime.UtcNow;
            balance.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.AccountBalances.UpdateAsync(balance);

            await _auditService.LogTransactionAsync(
                accountId: account.AccountId,
                transactionType: 2,
                amount: amount,
                balanceAfter: balance.CurrentBalance,
                reason: $"Admin deleted vehicle {TransactionReasonHelper.FormatChassis(chassis)} — refund",
                referenceType: "Vehicle",
                referenceId: vehicleId,
                remarks: reason,
                initiatedBy: deletedBy);
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AllocateShowroomVehicleCommandHandler : IRequestHandler<AllocateShowroomVehicleCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AllocateShowroomVehicleCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AllocateShowroomVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null)
                throw new InvalidOperationException("Vehicle not found.");

            if (vehicle.SubdealerId.HasValue)
                throw new InvalidOperationException("Vehicle is already assigned to a subdealer.");

            if (vehicle.Status != UnifiedVehicleStatus.ApprovedByDealer)
                throw new InvalidOperationException("Only approved showroom stock can be allocated to a subdealer.");

            var hasBooking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .Any(b => b.VehicleId == vehicle.VehicleId);
            if (hasBooking)
                throw new InvalidOperationException("Vehicle has an active booking and cannot be re-allocated.");

            var orgId = await SubdealerOrgService.GetOrgIdForUserAsync(_unitOfWork, request.SubdealerId);
            var walletUserId = orgId.HasValue
                ? await SubdealerOrgService.GetPrimaryUserIdForOrgAsync(_unitOfWork, orgId.Value) ?? request.SubdealerId
                : request.SubdealerId;

            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync())
                .Where(a => a.SubdealerId == walletUserId && a.IsActive)
                .ToList();
            var walletAccount = accounts.FirstOrDefault(SubdealerOrgService.IsMainAccount)
                ?? accounts.FirstOrDefault()
                ?? throw new InvalidOperationException("Subdealer has no active wallet account.");

            var balance = (await _unitOfWork.AccountBalances.GetAllAsync())
                .FirstOrDefault(b => b.SubdealerAccountId == walletAccount.AccountId)
                ?? throw new InvalidOperationException("Subdealer wallet balance not found.");

            if (balance.AvailableBalance < vehicle.CurrentPrice)
                throw new InvalidOperationException(
                    $"Insufficient balance. Available: ₹{balance.AvailableBalance:N2}, required: ₹{vehicle.CurrentPrice:N2}.");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                balance.CurrentBalance -= vehicle.CurrentPrice;
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                balance.LastTransactionDate = DateTime.UtcNow;
                balance.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.AccountBalances.UpdateAsync(balance);

                vehicle.SubdealerId = walletUserId;
                vehicle.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.Vehicles.UpdateAsync(vehicle);

                await VehicleAllocationHelper.LogSubdealerEventAsync(
                    _unitOfWork, vehicle.VehicleId, "AllocatedToSubdealer", request.AllocatedBy, request.Remarks);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var targetUser = await _unitOfWork.Users.GetByIdAsync(walletUserId);
                var subdealerLabel = targetUser?.GetFullName() ?? $"Subdealer #{walletUserId}";

                await _auditService.LogTransactionAsync(
                    accountId: walletAccount.AccountId,
                    transactionType: 1,
                    amount: vehicle.CurrentPrice,
                    balanceAfter: balance.CurrentBalance,
                    reason: TransactionReasonHelper.ShowroomAllocation(vehicle.ChassisNumber),
                    referenceType: "Vehicle",
                    referenceId: vehicle.VehicleId,
                    remarks: request.Remarks,
                    initiatedBy: request.AllocatedBy);

                await _auditService.LogActionAsync(
                    entityType: "Vehicle",
                    entityId: vehicle.VehicleId,
                    action: "AllocateToSubdealer",
                    userId: request.AllocatedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        ChassisNumber = TransactionReasonHelper.FormatChassis(vehicle.ChassisNumber),
                        SubdealerId = walletUserId,
                        SubdealerName = subdealerLabel,
                        Amount = vehicle.CurrentPrice,
                        ReturnRequestId = request.ReturnRequestId
                    }),
                    remarks: request.Remarks);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error allocating vehicle: {ex.Message}", ex);
            }
        }
    }
}

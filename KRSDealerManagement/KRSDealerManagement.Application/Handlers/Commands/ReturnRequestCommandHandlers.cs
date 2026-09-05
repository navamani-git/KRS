using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateReturnRequestCommandHandler : IRequestHandler<CreateReturnRequestCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateReturnRequestCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateReturnRequestCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null)
                throw new InvalidOperationException("Vehicle not found.");

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == request.VehicleId);
            if (vehicle.Status == UnifiedVehicleStatus.Delivered)
                throw new InvalidOperationException("Delivered vehicles cannot be returned.");
            if (!UnifiedVehicleStatus.CanBookOrReturnPreInvoice(
                    vehicle.Status, booking != null, booking?.InvoiceDate)
                || !UnifiedVehicleStatus.CanRequestReturn(vehicle.Status))
                throw new InvalidOperationException(
                    "Return can only be requested after dealer approval and before customer booking or invoice.");

            var order = await _unitOfWork.PurchaseOrders.GetByIdAsync(request.OrderId);
            if (order?.CreatedByDealer == true)
                throw new InvalidOperationException("Dealer-created vehicles cannot be returned.");

            var existing = (await _unitOfWork.ReturnRequests.GetAllAsync())
                .Any(r => r.VehicleId == request.VehicleId && r.Status == 0);
            if (existing)
                throw new InvalidOperationException("A return request is already pending for this vehicle.");

            vehicle.Status = UnifiedVehicleStatus.ReturnRequested;
            vehicle.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

            var returnRequest = new ReturnRequest
            {
                AccountId = request.AccountId,
                OrderId = request.OrderId,
                VehicleId = request.VehicleId,
                RefundAmount = vehicle.CurrentPrice,
                ReturnReason = request.ReturnReason,
                Status = 0,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var returnId = await _unitOfWork.ReturnRequests.AddAsync(returnRequest);
            if (returnId <= 0)
                throw new InvalidOperationException("Failed to create return request (invalid ID). Please try again or contact support.");

            await VehicleHistoryHelper.LogSubdealerEventAsync(
                _unitOfWork, vehicle.VehicleId, "ReturnRequested", request.CreatedBy, request.ReturnReason);

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "ReturnRequest", entityId: returnId, action: "Create",
                userId: request.CreatedBy, userRole: "Subdealer",
                newValue: JsonSerializer.Serialize(new
                {
                    request.OrderId,
                    request.VehicleId,
                    ChassisNumber = TransactionReasonHelper.FormatChassis(vehicle.ChassisNumber),
                    SuggestedRefund = vehicle.CurrentPrice
                }));

            return returnId;
        }
    }

    public class ApproveReturnRequestCommandHandler : IRequestHandler<ApproveReturnRequestCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public ApproveReturnRequestCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(ApproveReturnRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var returnRequest = await _unitOfWork.ReturnRequests.GetByIdAsync(request.ReturnRequestId);
                if (returnRequest == null || returnRequest.Status != 0)
                    return false;

                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(returnRequest.VehicleId);
                if (vehicle == null)
                    return false;

                if (vehicle.Status == UnifiedVehicleStatus.Delivered)
                    return false;

                if (returnRequest.Status == 0 && vehicle.Status != UnifiedVehicleStatus.ReturnRequested)
                {
                    vehicle.Status = UnifiedVehicleStatus.ReturnRequested;
                    vehicle.DeliveryDate = null;
                    vehicle.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.Vehicles.UpdateAsync(vehicle);
                }
                else if (vehicle.Status != UnifiedVehicleStatus.ReturnRequested)
                    return false;

                var existingTransactions = (await _unitOfWork.AccountTransactions.GetAllAsync()).ToList();
                var refundAlreadyCredited = existingTransactions.Any(t =>
                    t.ReferenceType == "ReturnRequest"
                    && t.ReferenceId == returnRequest.ReturnRequestId
                    && t.AccountId == returnRequest.AccountId);

                returnRequest.RefundAmount = request.RefundAmount;
                returnRequest.Approve(request.ApprovedBy, request.Remarks);

                if (request.ReassignToSubdealerId.HasValue)
                {
                    var target = await _unitOfWork.Users.GetByIdAsync(request.ReassignToSubdealerId.Value);
                    if (target == null || !target.IsActive)
                        throw new InvalidOperationException("Selected subdealer is not available for reassignment.");

                    if (vehicle.SubdealerId == request.ReassignToSubdealerId.Value)
                        throw new InvalidOperationException("Vehicle is already assigned to this subdealer.");

                    vehicle.SubdealerId = request.ReassignToSubdealerId.Value;
                }
                else
                {
                    vehicle.SubdealerId = null;
                    if (vehicle.VehicleMasterId > 0)
                        await VehicleAllocationHelper.ReleaseMasterAsync(
                            _unitOfWork, vehicle.VehicleMasterId, request.ApprovedBy, request.Remarks);
                }

                vehicle.Status = UnifiedVehicleStatus.ApprovedByDealer;
                vehicle.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.Vehicles.UpdateAsync(vehicle);

                await VehicleAllocationHelper.LogSubdealerEventAsync(
                    _unitOfWork, vehicle.VehicleId, "ReturnApproved", request.ApprovedBy, request.Remarks);

                if (request.ReassignToSubdealerId.HasValue)
                {
                    await VehicleHistoryHelper.LogSubdealerEventAsync(
                        _unitOfWork, vehicle.VehicleId, "Reassigned", request.ApprovedBy,
                        $"Reassigned to subdealer #{request.ReassignToSubdealerId.Value}.");
                }

                await _unitOfWork.ReturnRequests.UpdateAsync(returnRequest);

                var balances = (await _unitOfWork.AccountBalances.GetAllAsync()).ToList();
                AccountBalance? balance = null;

                if (!refundAlreadyCredited)
                {
                    balance = balances.FirstOrDefault(b => b.SubdealerAccountId == returnRequest.AccountId);
                    if (balance != null)
                    {
                        balance.CurrentBalance += returnRequest.RefundAmount;
                        balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                        balance.LastTransactionDate = DateTime.UtcNow;
                        balance.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.AccountBalances.UpdateAsync(balance);
                    }
                }

                AccountBalance? targetBalance = null;
                int? targetAccountId = null;

                if (request.ReassignToSubdealerId.HasValue)
                {
                    var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync())
                        .Where(a => a.SubdealerId == request.ReassignToSubdealerId.Value && a.IsActive)
                        .ToList();
                    var targetAccount = accounts.FirstOrDefault()
                        ?? throw new InvalidOperationException("Selected subdealer has no active account.");

                    targetAccountId = targetAccount.AccountId;
                    targetBalance = balances.FirstOrDefault(b => b.SubdealerAccountId == targetAccount.AccountId)
                        ?? throw new InvalidOperationException("Target subdealer account balance not found.");

                    if (targetBalance.AvailableBalance < returnRequest.RefundAmount)
                        throw new InvalidOperationException(
                            $"Target subdealer has insufficient balance. Available: ₹{targetBalance.AvailableBalance:N2}, required: ₹{returnRequest.RefundAmount:N2}.");

                    targetBalance.CurrentBalance -= returnRequest.RefundAmount;
                    targetBalance.AvailableBalance = targetBalance.CurrentBalance - targetBalance.ReservedAmount;
                    targetBalance.LastTransactionDate = DateTime.UtcNow;
                    targetBalance.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.AccountBalances.UpdateAsync(targetBalance);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                string destinationLabel;
                if (request.ReassignToSubdealerId.HasValue)
                {
                    destinationLabel = $"subdealer #{request.ReassignToSubdealerId.Value}";
                }
                else
                {
                    var master = vehicle.VehicleMasterId > 0
                        ? await _unitOfWork.VehicleMasters.GetByIdAsync(vehicle.VehicleMasterId)
                        : null;
                    var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
                    var orgRoles = await _unitOfWork.UserOrgRoles.GetAllAsync();
                    var order = vehicle.PurchaseOrderId.HasValue
                        ? await _unitOfWork.PurchaseOrders.GetByIdAsync(vehicle.PurchaseOrderId.Value)
                        : null;
                    var account = await _unitOfWork.SubdealerAccounts.GetByIdAsync(returnRequest.AccountId);
                    destinationLabel = DealershipLocationHelper.ResolveShowroomLabel(
                        vehicle,
                        order?.SubdealerId,
                        account?.SubdealerId,
                        master != null
                            ? new Dictionary<int, VehicleMaster> { [master.VehicleMasterId] = master }
                            : new Dictionary<int, VehicleMaster>(),
                        dealerships,
                        orgRoles);
                }

                await _auditService.LogActionAsync(
                    entityType: "Vehicle",
                    entityId: vehicle.VehicleId,
                    action: request.ReassignToSubdealerId.HasValue ? "AllocateToSubdealer" : "ReturnToShowroom",
                    userId: request.ApprovedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        ReturnRequestId = returnRequest.ReturnRequestId,
                        ChassisNumber = TransactionReasonHelper.FormatChassis(vehicle.ChassisNumber),
                        RefundAmount = returnRequest.RefundAmount,
                        Destination = destinationLabel,
                        SubdealerId = request.ReassignToSubdealerId,
                        Remarks = request.Remarks
                    }),
                    remarks: request.Remarks);

                if (!refundAlreadyCredited)
                {
                    await _auditService.LogTransactionAsync(
                        accountId: returnRequest.AccountId, transactionType: 2,
                        amount: returnRequest.RefundAmount, balanceAfter: balance?.CurrentBalance ?? 0,
                        reason: TransactionReasonHelper.Return(vehicle.ChassisNumber),
                        referenceType: "ReturnRequest", referenceId: returnRequest.ReturnRequestId,
                        remarks: request.Remarks, initiatedBy: request.ApprovedBy);
                }

                if (targetAccountId.HasValue && targetBalance != null)
                {
                    await _auditService.LogTransactionAsync(
                        accountId: targetAccountId.Value, transactionType: 1,
                        amount: returnRequest.RefundAmount, balanceAfter: targetBalance.CurrentBalance,
                        reason: TransactionReasonHelper.Reassignment(vehicle.ChassisNumber),
                        referenceType: "ReturnRequest", referenceId: returnRequest.ReturnRequestId,
                        remarks: request.Remarks, initiatedBy: request.ApprovedBy);
                }

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error approving return: {ex.Message}", ex);
            }
        }
    }

    public class RejectReturnRequestCommandHandler : IRequestHandler<RejectReturnRequestCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public RejectReturnRequestCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(RejectReturnRequestCommand request, CancellationToken cancellationToken)
        {
            var returnRequest = await _unitOfWork.ReturnRequests.GetByIdAsync(request.ReturnRequestId);
            if (returnRequest == null || returnRequest.IsFinal())
                return false;

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(returnRequest.VehicleId);
            if (vehicle == null || vehicle.Status != UnifiedVehicleStatus.ReturnRequested)
                return false;

            vehicle.Status = UnifiedVehicleStatus.ApprovedByDealer;
            vehicle.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

            returnRequest.Reject(request.RejectedBy, request.Remarks);
            await _unitOfWork.ReturnRequests.UpdateAsync(returnRequest);

            await VehicleHistoryHelper.LogSubdealerEventAsync(
                _unitOfWork, vehicle.VehicleId, "ReturnRejected", request.RejectedBy, request.Remarks);

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "ReturnRequest", entityId: returnRequest.ReturnRequestId,
                action: "Reject", userId: request.RejectedBy, userRole: "Admin",
                newValue: JsonSerializer.Serialize(new { Remarks = request.Remarks }));

            return true;
        }
    }
}

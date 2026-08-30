using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AllocatePurchaseOrderItemsCommandHandler : IRequestHandler<AllocatePurchaseOrderItemsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AllocatePurchaseOrderItemsCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AllocatePurchaseOrderItemsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var order = await _unitOfWork.PurchaseOrders.GetByIdAsync(request.OrderId);
                if (order == null) return false;

                var lineItems = (await _unitOfWork.PurchaseOrderItems.GetByOrderIdAsync(request.OrderId)).ToList();
                var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
                var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
                var balance = await _unitOfWork.AccountBalances.GetByIdAsync(order.AccountId)
                    ?? throw new InvalidOperationException("Account balance not found.");

                var dealershipId = await ResolveDealershipIdAsync(order.SubdealerId);
                ValidateMasterSelections(request, lineItems, dealershipId);

                decimal approvedAmount = 0;
                int approvedCount = 0;
                decimal rejectedAmount = 0;
                int rejectedCount = 0;
                var approvedDebits = new List<(Vehicle Vehicle, PurchaseOrderItem Item, string ModelName, string ColorName)>();
                var orderVehicles = new List<Vehicle>();

                foreach (var alloc in request.Items)
                {
                    var item = lineItems.FirstOrDefault(x => x.OrderItemId == alloc.OrderItemId);
                    if (item == null || !item.CanBeApproved()) continue;

                    if (alloc.Approve)
                    {
                        if (!alloc.VehicleMasterId.HasValue || alloc.VehicleMasterId.Value <= 0)
                            throw new InvalidOperationException("Select a chassis from dealer stock for each approved line.");

                        var subdealerVehicleId = await VehicleAllocationHelper.AllocateFromMasterAsync(
                            _unitOfWork,
                            alloc.VehicleMasterId.Value,
                            item,
                            order.OrderId,
                            order.SubdealerId,
                            request.ApprovedBy,
                            UnifiedVehicleStatus.ApprovedByDealer,
                            item.UnitPrice,
                            alloc.Remarks ?? request.Remarks);

                        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(subdealerVehicleId)
                            ?? throw new InvalidOperationException("Failed to load allocated vehicle.");

                        item.Status = 1;
                        item.SubdealerVehicleId = subdealerVehicleId;
                        item.MotorNo = vehicle.MotorNo;
                        item.BatteryNo = vehicle.BatteryNo;
                        item.ChargerNo = vehicle.ChargerNo;
                        item.ControllerNo = vehicle.ControllerNo;
                        item.ConverterNo = vehicle.ConverterNo;
                        item.ChassisNumber = vehicle.ChassisNumber;
                        item.ApprovedBy = request.ApprovedBy;
                        item.ApprovedDate = DateTime.UtcNow;
                        item.Remarks = alloc.Remarks ?? request.Remarks;
                        item.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);

                        approvedAmount += item.UnitPrice;
                        approvedCount++;
                        models.TryGetValue(vehicle.ModelId, out var model);
                        colors.TryGetValue(vehicle.ColorId, out var color);
                        approvedDebits.Add((vehicle, item, model?.ModelName ?? "Unknown", color?.ColorName ?? "Unknown"));
                        orderVehicles.Add(vehicle);
                    }
                    else
                    {
                        item.Status = 2;
                        item.RejectedBy = request.ApprovedBy;
                        item.RejectedDate = DateTime.UtcNow;
                        item.Remarks = alloc.Remarks ?? request.Remarks;
                        item.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);

                        rejectedAmount += item.UnitPrice;
                        rejectedCount++;
                    }
                }

                if (approvedCount == 0 && rejectedCount == 0)
                    throw new InvalidOperationException("No pending line items were processed.");

                if (approvedAmount > 0)
                    balance.CurrentBalance -= approvedAmount;

                var releaseReserved = approvedAmount + rejectedAmount;
                balance.ReservedAmount = Math.Max(0, balance.ReservedAmount - releaseReserved);
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                balance.LastTransactionDate = DateTime.UtcNow;
                balance.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.AccountBalances.UpdateAsync(balance);

                var refreshedItems = (await _unitOfWork.PurchaseOrderItems.GetByOrderIdAsync(request.OrderId)).ToList();
                var allocatedVehicleIds = refreshedItems.Where(i => i.SubdealerVehicleId.HasValue).Select(i => i.SubdealerVehicleId!.Value).ToList();
                foreach (var vid in allocatedVehicleIds)
                {
                    var v = await _unitOfWork.Vehicles.GetByIdAsync(vid);
                    if (v != null) orderVehicles.Add(v);
                }

                int pendingLeft = refreshedItems.Count(i => i.Status == 0);
                int totalApproved = refreshedItems.Count(i => i.Status == 1);
                decimal totalApprovedAmt = refreshedItems.Where(i => i.Status == 1).Sum(i => i.UnitPrice);

                order.Status = VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, refreshedItems);
                order.ApprovedAmount = totalApprovedAmt;
                order.ApprovedVehicleCount = totalApproved;
                order.AdminNotes = request.Remarks;
                order.ApprovedBy = request.ApprovedBy;
                order.ApprovedDate = DateTime.UtcNow;
                order.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.PurchaseOrders.UpdateAsync(order);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                if (approvedAmount > 0)
                {
                    var runningBalance = balance.CurrentBalance + approvedAmount;
                    foreach (var (vehicle, item, modelName, colorName) in approvedDebits)
                    {
                        runningBalance -= item.UnitPrice;
                        var chassis = vehicle.ChassisNumber ?? item.ChassisNumber ?? "";
                        await _auditService.LogTransactionAsync(
                            accountId: order.AccountId,
                            transactionType: 1,
                            amount: item.UnitPrice,
                            balanceAfter: runningBalance,
                            reason: OrderTransactionReasonHelper.Format(
                                order.OrderNumber, chassis, modelName, colorName),
                            referenceType: "Vehicle",
                            referenceId: vehicle.VehicleId,
                            remarks: request.Remarks,
                            initiatedBy: request.ApprovedBy);
                    }
                }

                if (rejectedAmount > 0)
                {
                    await _auditService.LogTransactionAsync(
                        accountId: order.AccountId, transactionType: 4, amount: rejectedAmount,
                        balanceAfter: balance.CurrentBalance,
                        reason: $"Order {order.OrderNumber}: rejected {rejectedCount} vehicle(s)",
                        referenceType: "PurchaseOrder", referenceId: order.OrderId,
                        remarks: request.Remarks, initiatedBy: request.ApprovedBy);
                }

                await _auditService.LogActionAsync(
                    entityType: "PurchaseOrder", entityId: order.OrderId, action: "Allocate",
                    userId: request.ApprovedBy, userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new { Approved = approvedCount, Rejected = rejectedCount, PendingLeft = pendingLeft }));

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error allocating order items: {ex.Message}", ex);
            }
        }

        private static async Task<int> ResolveDealershipIdAsync(int subdealerId, IUnitOfWork unitOfWork)
        {
            var orgRole = (await unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == subdealerId && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .FirstOrDefault();
            if (orgRole?.DealershipId == null)
                throw new InvalidOperationException("Subdealer is not linked to a dealership.");
            return orgRole.DealershipId.Value;
        }

        private async Task<int> ResolveDealershipIdAsync(int subdealerId)
            => await ResolveDealershipIdAsync(subdealerId, _unitOfWork);

        private static void ValidateMasterSelections(
            AllocatePurchaseOrderItemsCommand request,
            List<PurchaseOrderItem> lineItems,
            int dealershipId)
        {
            var masterIds = request.Items
                .Where(i => i.Approve && i.VehicleMasterId.HasValue)
                .Select(i => i.VehicleMasterId!.Value)
                .ToList();

            var dup = masterIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dup.Count > 0)
                throw new InvalidOperationException("The same chassis cannot be allocated to multiple lines in one batch.");
        }
    }

    public class RejectPurchaseOrderItemsCommandHandler : IRequestHandler<RejectPurchaseOrderItemsCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public RejectPurchaseOrderItemsCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(RejectPurchaseOrderItemsCommand request, CancellationToken cancellationToken)
        {
            var pending = (await _unitOfWork.PurchaseOrderItems.GetPendingByOrderIdAsync(request.OrderId)).ToList();
            var targets = request.OrderItemIds == null || !request.OrderItemIds.Any()
                ? pending
                : pending.Where(p => request.OrderItemIds.Contains(p.OrderItemId)).ToList();

            if (!targets.Any()) return false;

            var items = targets.Select(t => new AllocateOrderItemDto
            {
                OrderItemId = t.OrderItemId,
                Approve = false,
                Remarks = request.Remarks
            }).ToList();

            return await new AllocatePurchaseOrderItemsCommandHandler(_unitOfWork, _auditService)
                .Handle(new AllocatePurchaseOrderItemsCommand
                {
                    OrderId = request.OrderId,
                    ApprovedBy = request.RejectedBy,
                    Remarks = request.Remarks,
                    Items = items
                }, cancellationToken);
        }
    }
}

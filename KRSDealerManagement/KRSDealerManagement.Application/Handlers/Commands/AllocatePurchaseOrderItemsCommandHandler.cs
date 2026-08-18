using MediatR;
using KRSDealerManagement.Application.Commands;
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
                var vehicles = await LoadVehiclesForLineItemsAsync(lineItems, _unitOfWork);
                var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
                var balance = await _unitOfWork.AccountBalances.GetByIdAsync(order.AccountId)
                    ?? throw new InvalidOperationException("Account balance not found.");

                ValidateChassisNumbers(request, lineItems, vehicles, await _unitOfWork.Vehicles.GetAllAsync());

                decimal approvedAmount = 0;
                int approvedCount = 0;
                decimal rejectedAmount = 0;
                int rejectedCount = 0;
                var approvedDebits = new List<(Vehicle Vehicle, PurchaseOrderItem Item, string ModelName)>();

                foreach (var alloc in request.Items)
                {
                    var item = lineItems.FirstOrDefault(x => x.OrderItemId == alloc.OrderItemId);
                    if (item == null || !item.CanBeApproved() || !item.VehicleId.HasValue) continue;

                    if (!vehicles.TryGetValue(item.VehicleId.Value, out var vehicle)) continue;

                    if (alloc.Approve)
                    {
                        var chassis = alloc.ChassisNumber!.Trim().ToUpperInvariant();
                        vehicle.ChassisNumber = chassis;
                        vehicle.Status = UnifiedVehicleStatus.ApprovedByDealer;
                        vehicle.MotorNo = alloc.MotorNo?.Trim();
                        vehicle.BatteryNo = alloc.BatteryNo?.Trim();
                        vehicle.ChargerNo = alloc.ChargerNo?.Trim();
                        vehicle.ControllerNo = alloc.ControllerNo?.Trim();
                        vehicle.ConverterNo = alloc.ConverterNo?.Trim();
                        vehicle.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.Vehicles.UpdateAsync(vehicle);

                        item.Status = 1;
                        item.MotorNo = alloc.MotorNo?.Trim();
                        item.BatteryNo = alloc.BatteryNo?.Trim();
                        item.ChargerNo = alloc.ChargerNo?.Trim();
                        item.ControllerNo = alloc.ControllerNo?.Trim();
                        item.ConverterNo = alloc.ConverterNo?.Trim();
                        item.ChassisNumber = chassis;
                        item.ApprovedBy = request.ApprovedBy;
                        item.ApprovedDate = DateTime.UtcNow;
                        item.Remarks = alloc.Remarks ?? request.Remarks;
                        item.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);

                        approvedAmount += item.UnitPrice;
                        approvedCount++;
                        models.TryGetValue(vehicle.ModelId, out var model);
                        approvedDebits.Add((vehicle, item, model?.ModelName ?? "Unknown"));
                    }
                    else
                    {
                        vehicle.Status = UnifiedVehicleStatus.RejectedByDealer;
                        vehicle.ModifiedDate = DateTime.UtcNow;
                        await _unitOfWork.Vehicles.UpdateAsync(vehicle);

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

                var orderVehicles = vehicles.Values.ToList();
                int pendingLeft = lineItems.Count(i => i.Status == 0);
                int totalApproved = lineItems.Count(i => i.Status == 1);
                decimal totalApprovedAmt = lineItems.Where(i => i.Status == 1).Sum(i => i.UnitPrice);

                order.Status = VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, lineItems);
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
                    // CurrentBalance already reduced by approvedAmount; start from pre-debit balance for running ledger.
                    var runningBalance = balance.CurrentBalance + approvedAmount;
                    foreach (var (vehicle, item, modelName) in approvedDebits)
                    {
                        runningBalance -= item.UnitPrice;
                        var chassis = vehicle.ChassisNumber ?? item.ChassisNumber ?? "";
                        await _auditService.LogTransactionAsync(
                            accountId: order.AccountId,
                            transactionType: 1,
                            amount: item.UnitPrice,
                            balanceAfter: runningBalance,
                            reason: $"Order {order.OrderNumber}: {modelName} — {chassis}",
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

        private static async Task<Dictionary<int, Vehicle>> LoadVehiclesForLineItemsAsync(
            List<PurchaseOrderItem> lineItems, IUnitOfWork unitOfWork)
        {
            var vehicles = new Dictionary<int, Vehicle>();
            foreach (var vehicleId in lineItems.Where(i => i.VehicleId.HasValue).Select(i => i.VehicleId!.Value).Distinct())
            {
                var vehicle = await unitOfWork.Vehicles.GetByIdAsync(vehicleId);
                if (vehicle != null)
                    vehicles[vehicleId] = vehicle;
            }

            return vehicles;
        }

        private static void ValidateChassisNumbers(
            AllocatePurchaseOrderItemsCommand request,
            List<PurchaseOrderItem> lineItems,
            Dictionary<int, Vehicle> orderVehicles,
            IEnumerable<Vehicle> allVehicles)
        {
            var approved = request.Items
                .Where(i => i.Approve && !string.IsNullOrWhiteSpace(i.ChassisNumber))
                .Select(i => i.ChassisNumber!.Trim().ToUpperInvariant())
                .ToList();

            var dupInForm = approved
                .GroupBy(c => c)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dupInForm.Count > 0)
                throw new InvalidOperationException(
                    $"Duplicate chassis number(s) in this allocation: {string.Join(", ", dupInForm)}.");

            var batchVehicleIds = new HashSet<int>();
            foreach (var alloc in request.Items.Where(i => i.Approve))
            {
                var item = lineItems.FirstOrDefault(x => x.OrderItemId == alloc.OrderItemId);
                if (item?.VehicleId is int vid)
                    batchVehicleIds.Add(vid);
            }

            foreach (var chassis in approved.Distinct())
            {
                var conflict = allVehicles.FirstOrDefault(v =>
                    !string.IsNullOrWhiteSpace(v.ChassisNumber)
                    && string.Equals(v.ChassisNumber.Trim(), chassis, StringComparison.OrdinalIgnoreCase)
                    && !batchVehicleIds.Contains(v.VehicleId));

                if (conflict != null)
                    throw new InvalidOperationException(
                        $"Chassis {chassis} already exists in the system (vehicle #{conflict.VehicleId}).");
            }
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

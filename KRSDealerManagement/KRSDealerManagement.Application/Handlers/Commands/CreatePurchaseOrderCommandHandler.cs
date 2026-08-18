using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Creates purchase order + line items + one vehicle per line at Submitted (#1).
    /// Staff AutoApprove: dealer-created PO, vehicles at Approved (#2) with chassis.
    /// </summary>
    public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreatePurchaseOrderCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var balances = await _unitOfWork.AccountBalances.GetAllAsync();
                var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == request.AccountId)
                    ?? throw new InvalidOperationException("Account balance not found.");

                decimal totalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity);
                int totalQty = request.Items.Sum(i => i.Quantity);

                if (!request.AutoApprove && balance.AvailableBalance < totalAmount)
                    throw new InvalidOperationException(
                        $"Insufficient balance. Available: ₹{balance.AvailableBalance:N2}, Required: ₹{totalAmount:N2}");

                foreach (var item in request.Items)
                    await ModelColorValidation.EnsureMappedAsync(_unitOfWork, item.ModelId, item.ColorId);

                var allOrders = await _unitOfWork.PurchaseOrders.GetAllAsync();
                string orderNumber = $"ORD-{DateTime.UtcNow.Year}-{(allOrders.Count() + 1):D5}";

                var order = new PurchaseOrder
                {
                    AccountId = request.AccountId,
                    SubdealerId = request.SubdealerId,
                    OrderNumber = orderNumber,
                    TotalQuantity = totalQty,
                    TotalAmount = totalAmount,
                    CreatedByDealer = request.AutoApprove,
                    Status = request.AutoApprove ? UnifiedVehicleStatus.ApprovedByDealer : UnifiedVehicleStatus.Submitted,
                    SubdealerNotes = request.SubdealerNotes,
                    AdminNotes = request.AutoApprove ? (request.AdminNotes ?? "Created by dealer") : null,
                    ApprovedBy = request.AutoApprove ? request.CreatedBy : null,
                    ApprovedDate = request.AutoApprove ? DateTime.UtcNow : null,
                    ApprovedAmount = request.AutoApprove ? totalAmount : 0,
                    ApprovedVehicleCount = request.AutoApprove ? totalQty : 0,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                var orderId = await _unitOfWork.PurchaseOrders.AddAsync(order);

                foreach (var group in request.Items)
                {
                    for (int i = 0; i < group.Quantity; i++)
                    {
                        var item = new PurchaseOrderItem
                        {
                            PurchaseOrderId = orderId,
                            ModelId = group.ModelId,
                            ColorId = group.ColorId,
                            UnitPrice = group.UnitPrice,
                            Status = request.AutoApprove ? 1 : 0,
                            ApprovedBy = request.AutoApprove ? request.CreatedBy : null,
                            ApprovedDate = request.AutoApprove ? DateTime.UtcNow : null,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow
                        };

                        var itemId = await _unitOfWork.PurchaseOrderItems.AddAsync(item);

                        if (request.AutoApprove)
                        {
                            if (string.IsNullOrWhiteSpace(group.ChassisNumber)
                                || string.IsNullOrWhiteSpace(group.MotorNo)
                                || string.IsNullOrWhiteSpace(group.BatteryNo))
                            {
                                throw new InvalidOperationException(
                                    "Chassis, motor, and battery numbers are required for dealer-created orders.");
                            }

                            var chassis = group.ChassisNumber.Trim().ToUpperInvariant();
                            var vehicleId = await CreateVehicleAsync(
                                item, orderId, request.SubdealerId, request.CreatedBy,
                                UnifiedVehicleStatus.ApprovedByDealer, chassis, group);

                            item.OrderItemId = itemId;
                            item.ChassisNumber = chassis;
                            item.MotorNo = group.MotorNo?.Trim();
                            item.BatteryNo = group.BatteryNo?.Trim();
                            item.ChargerNo = group.ChargerNo?.Trim();
                            item.ControllerNo = group.ControllerNo?.Trim();
                            item.ConverterNo = group.ConverterNo?.Trim();
                            item.VehicleId = vehicleId;
                            await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);
                        }
                        else
                        {
                            var placeholder = UnifiedVehicleStatus.PlaceholderChassis(orderId, itemId);
                            var vehicleId = await CreateVehicleAsync(
                                item, orderId, request.SubdealerId, request.CreatedBy,
                                UnifiedVehicleStatus.Submitted, placeholder, group);

                            item.OrderItemId = itemId;
                            item.VehicleId = vehicleId;
                            item.ChassisNumber = placeholder;
                            await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);
                        }
                    }
                }

                if (request.AutoApprove)
                {
                    balance.CurrentBalance -= totalAmount;
                    balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                }
                else
                {
                    balance.ReservedAmount += totalAmount;
                    balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
                }

                balance.LastTransactionDate = DateTime.UtcNow;
                balance.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.AccountBalances.UpdateAsync(balance);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                if (request.AutoApprove)
                {
                    await _auditService.LogTransactionAsync(
                        accountId: request.AccountId, transactionType: 1, amount: totalAmount,
                        balanceAfter: balance.CurrentBalance,
                        reason: $"Dealer order {orderNumber} ({totalQty} vehicles)",
                        referenceType: "PurchaseOrder", referenceId: orderId,
                        remarks: request.AdminNotes, initiatedBy: request.CreatedBy);
                }
                else
                {
                    await _auditService.LogTransactionAsync(
                        accountId: request.AccountId, transactionType: 3, amount: totalAmount,
                        balanceAfter: balance.CurrentBalance,
                        reason: $"Reserved for order {orderNumber} ({totalQty} vehicles)",
                        referenceType: "PurchaseOrder", referenceId: orderId, initiatedBy: request.CreatedBy);
                }

                await _auditService.LogActionAsync(
                    entityType: "PurchaseOrder", entityId: orderId,
                    action: request.AutoApprove ? "Create_Dealer" : "Create",
                    userId: request.CreatedBy,
                    userRole: request.AutoApprove ? "Staff" : "Subdealer",
                    newValue: JsonSerializer.Serialize(new { orderNumber, totalQty, totalAmount, request.AutoApprove }));

                return orderId;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error creating purchase order: {ex.Message}", ex);
            }
        }

        private async Task<int> CreateVehicleAsync(
            PurchaseOrderItem item, int orderId, int subdealerId, int createdBy,
            int status, string chassis, OrderItem group)
        {
            return await _unitOfWork.Vehicles.AddAsync(new Vehicle
            {
                ModelId = item.ModelId,
                ColorId = item.ColorId,
                ChassisNumber = chassis,
                Status = status,
                PurchaseOrderId = orderId,
                SubdealerId = subdealerId,
                CurrentPrice = item.UnitPrice,
                OriginalPrice = item.UnitPrice,
                MotorNo = group.MotorNo?.Trim(),
                BatteryNo = group.BatteryNo?.Trim(),
                ChargerNo = group.ChargerNo?.Trim(),
                ControllerNo = group.ControllerNo?.Trim(),
                ConverterNo = group.ConverterNo?.Trim(),
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
        }
    }
}

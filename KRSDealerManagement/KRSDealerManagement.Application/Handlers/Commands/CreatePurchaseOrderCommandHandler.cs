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
    /// Creates purchase order + line items. Vehicles are created only on allocation (or dealer auto-approve from master).
    /// </summary>
    public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IVehiclePriceService _priceService;

        public CreatePurchaseOrderCommandHandler(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IVehiclePriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _priceService = priceService;
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

                var createDate = DateTime.Today;
                foreach (var item in request.Items)
                {
                    var priceError = await _priceService.ValidatePriceForVehicleCreateAsync(
                        item.ModelId, item.ColorId, createDate);
                    if (priceError != null)
                        throw new InvalidOperationException(priceError);
                }

                var dealershipId = await ResolveDealershipIdAsync(request.SubdealerId);

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

                var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
                var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
                var autoApproveDebits = new List<(int SubdealerVehicleId, string Chassis, int ModelId, int ColorId, decimal Amount)>();

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
                        item.OrderItemId = itemId;

                        if (request.AutoApprove)
                        {
                            if (!group.VehicleMasterId.HasValue || group.VehicleMasterId.Value <= 0)
                                throw new InvalidOperationException("Select a chassis from dealer stock for dealer-created orders.");

                            var subdealerVehicleId = await VehicleAllocationHelper.AllocateFromMasterAsync(
                                _unitOfWork,
                                group.VehicleMasterId.Value,
                                item,
                                orderId,
                                request.SubdealerId,
                                request.CreatedBy,
                                UnifiedVehicleStatus.ApprovedByDealer,
                                item.UnitPrice,
                                request.AdminNotes);

                            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(group.VehicleMasterId.Value);
                            item.SubdealerVehicleId = subdealerVehicleId;
                            item.ChassisNumber = master?.ChassisNumber;
                            item.MotorNo = master?.MotorNo;
                            item.BatteryNo = master?.BatteryNo;
                            item.ChargerNo = master?.ChargerNo;
                            item.ControllerNo = master?.ControllerNo;
                            item.ConverterNo = master?.ConverterNo;
                            await _unitOfWork.PurchaseOrderItems.UpdateAsync(item);

                            autoApproveDebits.Add((subdealerVehicleId, master?.ChassisNumber ?? "", item.ModelId, item.ColorId, item.UnitPrice));
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

                if (request.AutoApprove && autoApproveDebits.Count > 0)
                {
                    var runningBalance = balance.CurrentBalance + totalAmount;
                    foreach (var debit in autoApproveDebits)
                    {
                        runningBalance -= debit.Amount;
                        models.TryGetValue(debit.ModelId, out var model);
                        colors.TryGetValue(debit.ColorId, out var color);
                        await _auditService.LogTransactionAsync(
                            accountId: request.AccountId,
                            transactionType: 1,
                            amount: debit.Amount,
                            balanceAfter: runningBalance,
                            reason: OrderTransactionReasonHelper.Format(
                                orderNumber,
                                debit.Chassis,
                                model?.ModelName ?? "Unknown",
                                color?.ColorName ?? "Unknown"),
                            referenceType: "Vehicle",
                            referenceId: debit.SubdealerVehicleId,
                            remarks: request.AdminNotes,
                            initiatedBy: request.CreatedBy);
                    }
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

        private async Task<int> ResolveDealershipIdAsync(int subdealerId)
        {
            var orgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == subdealerId && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .FirstOrDefault();
            if (orgRoles?.DealershipId == null)
                throw new InvalidOperationException("Subdealer is not linked to a dealership.");
            return orgRoles.DealershipId.Value;
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class AdminCorrectVehicleCommandHandler : IRequestHandler<AdminCorrectVehicleCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IAuditService _auditService;

        public AdminCorrectVehicleCommandHandler(IUnitOfWork unitOfWork, IMediator mediator, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _auditService = auditService;
        }

        public async Task<bool> Handle(AdminCorrectVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null) return false;

            var oldVehicleStatus = vehicle.Status;
            var pendingReturns = (await _unitOfWork.ReturnRequests.GetAllAsync())
                .Where(r => r.VehicleId == vehicle.VehicleId && r.Status == 0)
                .ToList();

            await ModelColorValidation.EnsureMappedAsync(_unitOfWork, request.ModelId, request.ColorId);

            var labels = await CorrectionNoteLabelResolver.LoadAsync(_unitOfWork);
            var changes = new List<string>();
            var oldPrice = vehicle.CurrentPrice;
            var oldSubdealerId = vehicle.SubdealerId;
            var oldDeliveryDate = vehicle.DeliveryDate;

            if (vehicle.ModelId != request.ModelId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Model", labels.Model(vehicle.ModelId), labels.Model(request.ModelId)));
            if (vehicle.ColorId != request.ColorId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Colour", labels.Color(vehicle.ColorId), labels.Color(request.ColorId)));
            if (!string.Equals(vehicle.ChassisNumber?.Trim(), request.ChassisNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Chassis number", vehicle.ChassisNumber, request.ChassisNumber.Trim().ToUpperInvariant()));
            if (vehicle.Status != request.Status)
                changes.Add(CorrectionNoteHelper.DescribeChange("Vehicle status", labels.VehicleStatus(vehicle.Status), labels.VehicleStatus(request.Status)));
            if (vehicle.CurrentPrice != request.CurrentPrice)
                changes.Add(CorrectionNoteHelper.DescribeChange("Price", $"₹{vehicle.CurrentPrice:N2}", $"₹{request.CurrentPrice:N2}"));
            if (vehicle.SubdealerId != request.SubdealerId)
                changes.Add(CorrectionNoteHelper.DescribeChange("Assigned to", labels.Subdealer(vehicle.SubdealerId), labels.Subdealer(request.SubdealerId)));
            if (vehicle.DeliveryDate?.Date != request.DeliveryDate?.Date)
                changes.Add(CorrectionNoteHelper.DescribeChange("Delivery date",
                    CorrectionNoteLabelResolver.DateTimeValue(oldDeliveryDate),
                    CorrectionNoteLabelResolver.DateTimeValue(request.DeliveryDate)));
            if (!string.Equals(vehicle.MotorNo, request.MotorNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Motor number", vehicle.MotorNo, request.MotorNo));
            if (!string.Equals(vehicle.BatteryNo, request.BatteryNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Battery number", vehicle.BatteryNo, request.BatteryNo));
            if (!string.Equals(vehicle.ChargerNo, request.ChargerNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Charger number", vehicle.ChargerNo, request.ChargerNo));
            if (!string.Equals(vehicle.ControllerNo, request.ControllerNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Controller number", vehicle.ControllerNo, request.ControllerNo));
            if (!string.Equals(vehicle.ConverterNo, request.ConverterNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Converter number", vehicle.ConverterNo, request.ConverterNo));

            if (request.BookingStatus.HasValue)
            {
                var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
                if (booking != null && booking.BookingStatus != request.BookingStatus.Value)
                    changes.Add(CorrectionNoteHelper.DescribeChange(
                        "Booking stage",
                        labels.VehicleStatus(booking.BookingStatus),
                        labels.VehicleStatus(request.BookingStatus.Value)));
            }

            var noteEntry = CorrectionNoteHelper.FormatEntry(request.CorrectedByName, request.CorrectionReason, changes);

            if (vehicle.VehicleMasterId > 0)
            {
                var master = await _unitOfWork.VehicleMasters.GetByIdAsync(vehicle.VehicleMasterId)
                    ?? throw new InvalidOperationException("Linked vehicle master record not found.");

                var newChassis = request.ChassisNumber.Trim().ToUpperInvariant();
                if (!string.Equals(master.ChassisNumber, newChassis, StringComparison.OrdinalIgnoreCase))
                    await GlobalUniqueValidation.EnsureChassisAvailableAsync(
                        _unitOfWork, newChassis, master.VehicleMasterId, vehicle.VehicleId);

                master.ModelId = request.ModelId;
                master.ColorId = request.ColorId;
                master.ChassisNumber = newChassis;
                master.MotorNo = request.MotorNo?.Trim() ?? "";
                master.BatteryNo = request.BatteryNo?.Trim() ?? "";
                master.ChargerNo = request.ChargerNo?.Trim() ?? "";
                master.ControllerNo = request.ControllerNo?.Trim() ?? "";
                master.ConverterNo = request.ConverterNo?.Trim() ?? "";
                master.ModifiedBy = request.CorrectedBy;
                master.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.VehicleMasters.UpdateAsync(master);
                await _unitOfWork.VehicleMasters.AddHistoryAsync(new Domain.Entities.VehicleMasterHistory
                {
                    VehicleMasterId = master.VehicleMasterId,
                    Action = "Edited",
                    Remarks = noteEntry,
                    UserId = request.CorrectedBy
                });

                vehicle.ModelId = master.ModelId;
                vehicle.ColorId = master.ColorId;
                vehicle.ChassisNumber = master.ChassisNumber;
                vehicle.MotorNo = master.MotorNo;
                vehicle.BatteryNo = master.BatteryNo;
                vehicle.ChargerNo = master.ChargerNo;
                vehicle.ControllerNo = master.ControllerNo;
                vehicle.ConverterNo = master.ConverterNo;
            }

            vehicle.Status = request.Status;
            vehicle.CurrentPrice = request.CurrentPrice;
            vehicle.SubdealerId = request.SubdealerId;

            if (pendingReturns.Count > 0)
            {
                var adminMovedOffReturn = oldVehicleStatus == UnifiedVehicleStatus.ReturnRequested
                    && request.Status != UnifiedVehicleStatus.ReturnRequested;
                var adminChoseDifferentNonReturnStatus = request.Status != UnifiedVehicleStatus.ReturnRequested
                    && request.Status != oldVehicleStatus;

                if (adminMovedOffReturn || adminChoseDifferentNonReturnStatus)
                {
                    var cancelNote = $"Return cancelled due to admin correction: {request.CorrectionReason}";
                    foreach (var pendingReturn in pendingReturns)
                    {
                        pendingReturn.Reject(request.CorrectedBy, cancelNote);
                        await _unitOfWork.ReturnRequests.UpdateAsync(pendingReturn);
                    }

                    await VehicleHistoryHelper.LogSubdealerEventAsync(
                        _unitOfWork, vehicle.VehicleId, "ReturnRejected", request.CorrectedBy, cancelNote);

                    vehicle.DeliveryDate = request.Status == UnifiedVehicleStatus.Delivered
                        ? (request.DeliveryDate ?? DateTime.UtcNow)
                        : null;
                }
                else
                {
                    vehicle.Status = UnifiedVehicleStatus.ReturnRequested;
                    vehicle.DeliveryDate = null;
                }
            }
            else
            {
                vehicle.DeliveryDate = request.Status == UnifiedVehicleStatus.Delivered
                    ? (request.DeliveryDate ?? DateTime.UtcNow)
                    : null;
            }

            vehicle.ModifiedDate = DateTime.UtcNow;
            vehicle.Notes = CorrectionNoteHelper.Append(vehicle.Notes, noteEntry);

            await _unitOfWork.Vehicles.UpdateAsync(vehicle);
            await VehicleAllocationHelper.LogSubdealerEventAsync(
                _unitOfWork, vehicle.VehicleId, "Edited", request.CorrectedBy, noteEntry);

            if (request.BookingStatus.HasValue)
            {
                var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
                if (booking != null && booking.BookingStatus != request.BookingStatus.Value)
                {
                    await _unitOfWork.UpdateVehicleBookingStatusAsync(
                        booking.VehicleBookingId, request.BookingStatus.Value, request.CorrectedBy);
                }
            }

            if (oldSubdealerId != request.SubdealerId)
            {
                if (oldSubdealerId.HasValue && oldPrice > 0)
                {
                    await AdjustSubdealerBalanceAsync(
                        oldSubdealerId.Value,
                        -oldPrice,
                        vehicle.VehicleId,
                        vehicle.ChassisNumber,
                        request.CorrectedBy,
                        $"Admin transfer out — {noteEntry}");
                }

                if (request.SubdealerId.HasValue && request.CurrentPrice > 0)
                {
                    await AdjustSubdealerBalanceAsync(
                        request.SubdealerId.Value,
                        request.CurrentPrice,
                        vehicle.VehicleId,
                        vehicle.ChassisNumber,
                        request.CorrectedBy,
                        $"Admin transfer in — {noteEntry}");
                }
            }
            else if (request.CurrentPrice != oldPrice && request.SubdealerId.HasValue)
            {
                var delta = request.CurrentPrice - oldPrice;
                await AdjustSubdealerBalanceAsync(
                    request.SubdealerId.Value,
                    delta,
                    vehicle.VehicleId,
                    vehicle.ChassisNumber,
                    request.CorrectedBy,
                    noteEntry);
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "Vehicle",
                entityId: vehicle.VehicleId,
                action: "AdminCorrection",
                userId: request.CorrectedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    request.CorrectionReason,
                    changes,
                    vehicle.ChassisNumber,
                    request.SubdealerId,
                    request.DeliveryDate
                }),
                remarks: noteEntry);

            return true;
        }

        private async Task AdjustSubdealerBalanceAsync(
            int subdealerId, decimal delta, int vehicleId, string chassis, int correctedBy, string note)
        {
            if (delta == 0) return;

            var accounts = await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = subdealerId, IsActive = true });
            var account = accounts.FirstOrDefault();
            if (account == null) return;

            var balance = (await _unitOfWork.AccountBalances.GetAllAsync())
                .FirstOrDefault(b => b.SubdealerAccountId == account.AccountId);
            if (balance == null) return;

            if (delta > 0)
            {
                balance.CurrentBalance -= delta;
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            }
            else
            {
                balance.CurrentBalance += Math.Abs(delta);
                balance.AvailableBalance = balance.CurrentBalance - balance.ReservedAmount;
            }
            balance.LastTransactionDate = DateTime.UtcNow;
            balance.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.AccountBalances.UpdateAsync(balance);

            await _auditService.LogTransactionAsync(
                accountId: account.AccountId,
                transactionType: delta > 0 ? 1 : 2,
                amount: Math.Abs(delta),
                balanceAfter: balance.CurrentBalance,
                reason: $"Admin vehicle correction for chassis {chassis}",
                referenceType: "Vehicle",
                referenceId: vehicleId,
                remarks: note,
                initiatedBy: correctedBy);

            await AccountTransactionBalanceRecalcHelper.RecalculateAccountAsync(_unitOfWork, account.AccountId);
        }
    }
}

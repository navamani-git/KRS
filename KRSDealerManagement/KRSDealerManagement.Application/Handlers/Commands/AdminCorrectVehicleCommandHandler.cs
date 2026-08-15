using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Enums;
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

            var changes = new List<string>();
            var oldPrice = vehicle.CurrentPrice;

            if (vehicle.ModelId != request.ModelId)
                changes.Add(CorrectionNoteHelper.DescribeChange("ModelId", vehicle.ModelId, request.ModelId));
            if (vehicle.ColorId != request.ColorId)
                changes.Add(CorrectionNoteHelper.DescribeChange("ColorId", vehicle.ColorId, request.ColorId));
            if (!string.Equals(vehicle.ChassisNumber?.Trim(), request.ChassisNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Chassis", vehicle.ChassisNumber, request.ChassisNumber.Trim().ToUpperInvariant()));
            if (vehicle.Status != request.Status)
                changes.Add(CorrectionNoteHelper.DescribeChange("Vehicle Status", vehicle.Status, request.Status));
            if (vehicle.CurrentPrice != request.CurrentPrice)
                changes.Add(CorrectionNoteHelper.DescribeChange("Price", $"₹{vehicle.CurrentPrice:N2}", $"₹{request.CurrentPrice:N2}"));
            if (!string.Equals(vehicle.MotorNo, request.MotorNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Motor", vehicle.MotorNo, request.MotorNo));
            if (!string.Equals(vehicle.BatteryNo, request.BatteryNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Battery", vehicle.BatteryNo, request.BatteryNo));
            if (!string.Equals(vehicle.ChargerNo, request.ChargerNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Charger", vehicle.ChargerNo, request.ChargerNo));
            if (!string.Equals(vehicle.ControllerNo, request.ControllerNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Controller", vehicle.ControllerNo, request.ControllerNo));
            if (!string.Equals(vehicle.ConverterNo, request.ConverterNo, StringComparison.OrdinalIgnoreCase))
                changes.Add(CorrectionNoteHelper.DescribeChange("Converter", vehicle.ConverterNo, request.ConverterNo));

            if (request.BookingStatus.HasValue)
            {
                var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
                if (booking != null && booking.BookingStatus != request.BookingStatus.Value)
                    changes.Add(CorrectionNoteHelper.DescribeChange("Booking Status", booking.BookingStatus, request.BookingStatus.Value));
            }

            var noteEntry = CorrectionNoteHelper.FormatEntry(request.CorrectedByName, request.CorrectionReason, changes);

            vehicle.ModelId = request.ModelId;
            vehicle.ColorId = request.ColorId;
            vehicle.ChassisNumber = request.ChassisNumber.Trim().ToUpperInvariant();
            vehicle.Status = request.Status;
            vehicle.CurrentPrice = request.CurrentPrice;
            vehicle.MotorNo = request.MotorNo?.Trim();
            vehicle.BatteryNo = request.BatteryNo?.Trim();
            vehicle.ChargerNo = request.ChargerNo?.Trim();
            vehicle.ControllerNo = request.ControllerNo?.Trim();
            vehicle.ConverterNo = request.ConverterNo?.Trim();
            vehicle.ModifiedDate = DateTime.UtcNow;
            vehicle.Notes = CorrectionNoteHelper.Append(vehicle.Notes, noteEntry);

            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

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

            if (vehicle.CurrentPrice != oldPrice && vehicle.SubdealerId.HasValue)
            {
                var delta = vehicle.CurrentPrice - oldPrice;
                await AdjustSubdealerBalanceAsync(
                    vehicle.SubdealerId.Value,
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
                    vehicle.ChassisNumber
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
        }
    }
}

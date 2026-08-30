using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class MarkVehicleDeliveredCommandHandler : IRequestHandler<MarkVehicleDeliveredCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;

        public MarkVehicleDeliveredCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(MarkVehicleDeliveredCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null)
                throw new InvalidOperationException("Vehicle not found.");

            if (vehicle.Status == UnifiedVehicleStatus.Delivered)
                throw new InvalidOperationException("Vehicle is already marked as delivered.");

            if (vehicle.Status == UnifiedVehicleStatus.RejectedByDealer)
                throw new InvalidOperationException("This vehicle was rejected by the dealer and cannot be marked as delivered.");

            if (!vehicle.SubdealerId.HasValue || vehicle.SubdealerId.Value != request.MarkedBy)
                throw new InvalidOperationException("You can only mark delivery for your own vehicles.");

            var deliveryAt = request.DeliveryDate;
            var deliveryDay = deliveryAt.Date;
            var today = DateTime.UtcNow.Date;
            if (deliveryDay > today)
                throw new InvalidOperationException("Delivery date cannot be in the future.");

            DateTime? orderDate = null;
            if (vehicle.PurchaseOrderId.HasValue)
            {
                var order = await _unitOfWork.PurchaseOrders.GetByIdAsync(vehicle.PurchaseOrderId.Value);
                orderDate = order?.CreatedDate.Date;
            }

            if (orderDate.HasValue && deliveryDay < orderDate.Value)
                throw new InvalidOperationException("Delivery date cannot be before the order date.");

            vehicle.Status = UnifiedVehicleStatus.Delivered;
            vehicle.DeliveryDate = deliveryAt;
            vehicle.ModifiedBy = request.MarkedBy;
            vehicle.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

            var booking = request.VehicleBookingId.HasValue
                ? await _unitOfWork.VehicleBookings.GetByIdAsync(request.VehicleBookingId.Value)
                : (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);

            if (booking != null)
            {
                booking.BookingStatus = UnifiedVehicleStatus.Delivered;
                booking.ModifiedBy = request.MarkedBy;
                booking.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.VehicleBookings.UpdateAsync(booking);
            }

            await VehicleHistoryHelper.LogSubdealerEventAsync(
                _unitOfWork,
                vehicle.VehicleId,
                "Delivered",
                request.MarkedBy,
                vehicle.DeliveryDate.HasValue
                    ? $"Delivered on {vehicle.DeliveryDate:yyyy-MM-dd HH:mm}."
                    : null);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}

using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class MarkVehicleDeliveredCommand : IRequest<bool>
    {
        public int VehicleId { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int MarkedBy { get; set; }
        public int? VehicleBookingId { get; set; }
    }
}

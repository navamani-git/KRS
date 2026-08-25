using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.DTOs
{
    public class VehicleBookingGridRowDto
    {
        public required VehicleBooking Booking { get; init; }
        public int VehicleId { get; init; }
        public required string Chassis { get; init; }
        public required string Subdealer { get; init; }
        public required string StatusName { get; init; }
        public int VehicleStatus { get; init; }
    }
}

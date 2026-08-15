using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class VehicleBookingRepository : Repository<VehicleBooking>
    {
        public VehicleBookingRepository(ApplicationDbContext context)
            : base(context, "VehicleBookings", "VehicleBookingId") { }

        public async Task UpdateStatusAsync(int bookingId, int bookingStatus, int? modifiedBy)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            await connection.ExecuteAsync(@"
UPDATE VehicleBookings SET
    BookingStatus = @BookingStatus,
    ModifiedBy = @ModifiedBy,
    ModifiedDate = @ModifiedDate
WHERE VehicleBookingId = @VehicleBookingId",
                new
                {
                    VehicleBookingId = bookingId,
                    BookingStatus = bookingStatus,
                    ModifiedBy = modifiedBy,
                    ModifiedDate = DateTime.UtcNow
                });
        }
    }
}

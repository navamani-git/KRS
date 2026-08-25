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
            await WithConnectionAsync(async (connection, transaction) =>
            {
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
                    }, transaction);
                return true;
            });
        }

        public async Task<VehicleBooking?> GetByStoredFilePathAsync(string path)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryFirstOrDefaultAsync<VehicleBooking>(@"
SELECT TOP 1 * FROM VehicleBookings
WHERE EAadhaarPath = @Path OR DocumentPath = @Path OR GstCertificatePath = @Path
   OR CustomerPhotoPath = @Path OR ChassisPhotoPath = @Path OR CustomerSignPath = @Path
   OR FaceVerificationPath = @Path OR RcImagePath = @Path OR BoothPhotoPath = @Path
   OR SubsidyUndertakingPath = @Path OR InvoicePath = @Path OR InsurancePath = @Path",
                new { Path = path });
        }
    }
}

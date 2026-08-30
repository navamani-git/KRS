using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class VehicleBookingRepository : Repository<VehicleBooking>
    {
        public VehicleBookingRepository(ApplicationDbContext context)
            : base(context, "VehicleBookings", "VehicleBookingId") { }

        public override async Task<IEnumerable<VehicleBooking>> GetAllAsync()
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryAsync<VehicleBooking>(SelectSql, transaction: transaction));
        }

        public override async Task<VehicleBooking> GetByIdAsync(int id)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<VehicleBooking>(
                    SelectSql + " WHERE VehicleBookingId = @Id", new { Id = id }, transaction));
        }

        public override async Task<int> AddAsync(VehicleBooking entity)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
INSERT INTO VehicleBookings (
    SubdealerVehicleId, SubdealerId, BookingStatus, CustomerName, IsCompanyBooking,
    CustomerMobile, AlternativeMobile, CustomerEmail, EAadhaarPath, EAadhaarPassword,
    DocumentTypeId, DocumentPath, GstCertificatePath, CustomerPhotoPath, ChassisPhotoPath,
    CustomerSignPath, RtoLocationId, FancyNumber, PaymentMode, FinanceNameId,
    NomineeName, NomineeDob, NomineeRelationship, SubmittedDate, CreatedBy, CreatedDate, ModifiedDate
)
VALUES (
    @SubdealerVehicleId, @SubdealerId, @BookingStatus, @CustomerName, @IsCompanyBooking,
    @CustomerMobile, @AlternativeMobile, @CustomerEmail, @EAadhaarPath, @EAadhaarPassword,
    @DocumentTypeId, @DocumentPath, @GstCertificatePath, @CustomerPhotoPath, @ChassisPhotoPath,
    @CustomerSignPath, @RtoLocationId, @FancyNumber, @PaymentMode, @FinanceNameId,
    @NomineeName, @NomineeDob, @NomineeRelationship, @SubmittedDate, @CreatedBy, @CreatedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    SubdealerVehicleId = entity.VehicleId,
                    entity.SubdealerId,
                    entity.BookingStatus,
                    entity.CustomerName,
                    entity.IsCompanyBooking,
                    entity.CustomerMobile,
                    entity.AlternativeMobile,
                    entity.CustomerEmail,
                    entity.EAadhaarPath,
                    entity.EAadhaarPassword,
                    entity.DocumentTypeId,
                    entity.DocumentPath,
                    entity.GstCertificatePath,
                    entity.CustomerPhotoPath,
                    entity.ChassisPhotoPath,
                    entity.CustomerSignPath,
                    entity.RtoLocationId,
                    entity.FancyNumber,
                    entity.PaymentMode,
                    entity.FinanceNameId,
                    entity.NomineeName,
                    entity.NomineeDob,
                    entity.NomineeRelationship,
                    entity.SubmittedDate,
                    entity.CreatedBy,
                    CreatedDate = entity.CreatedDate == default ? DateTime.UtcNow : entity.CreatedDate,
                    ModifiedDate = entity.ModifiedDate == default ? DateTime.UtcNow : entity.ModifiedDate
                }, transaction);
            });
        }

        private const string SelectSql = @"
SELECT
    VehicleBookingId,
    SubdealerVehicleId,
    SubdealerVehicleId AS VehicleId,
    SubdealerId, BookingStatus, CustomerName, IsCompanyBooking,
    CustomerMobile, AlternativeMobile, CustomerEmail, EAadhaarPath, EAadhaarPassword,
    DocumentTypeId, DocumentPath, GstCertificatePath, CustomerPhotoPath, ChassisPhotoPath,
    CustomerSignPath, RtoLocationId, FancyNumber, PaymentMode, FinanceNameId,
    NomineeName, NomineeDob, NomineeRelationship, SubmittedDate, PaperReceivedDate,
    InvoiceDate, InvoicePath, InsuranceDate, InsurancePath, AgentDate, RegistrationDate,
    RtoNumber, NumberPlateReceivedDate, NumberPlateReceivedBy, SubsidyId, SubsidyCustomerNameCaps,
    FaceVerificationPath, RcImagePath, BoothPhotoPath, SubsidyUndertakingPath, SubsidyDocsSubmittedDate,
    CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
FROM VehicleBookings";

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
            return await connection.QueryFirstOrDefaultAsync<VehicleBooking>(SelectSql + @"
 WHERE EAadhaarPath = @Path OR DocumentPath = @Path OR GstCertificatePath = @Path
   OR CustomerPhotoPath = @Path OR ChassisPhotoPath = @Path OR CustomerSignPath = @Path
   OR FaceVerificationPath = @Path OR RcImagePath = @Path OR BoothPhotoPath = @Path
   OR SubsidyUndertakingPath = @Path OR InvoicePath = @Path OR InsurancePath = @Path",
                new { Path = path });
        }
    }
}

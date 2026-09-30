using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class WarrantyCreditNoteRepository : Repository<WarrantyCreditNote>, IWarrantyCreditNoteRepository
    {
        public WarrantyCreditNoteRepository(ApplicationDbContext context)
            : base(context, "WarrantyCreditNotes", "WarrantyClaimId")
        {
        }

        public async Task<WarrantyCreditNote?> GetByClaimIdAsync(int warrantyClaimId)
        {
            return await WithConnectionAsync(async (connection, transaction) =>
                await connection.QueryFirstOrDefaultAsync<WarrantyCreditNote>(
                    "SELECT * FROM WarrantyCreditNotes WHERE WarrantyClaimId = @WarrantyClaimId",
                    new { WarrantyClaimId = warrantyClaimId },
                    transaction));
        }

        public async Task UpsertAsync(WarrantyCreditNote creditNote, int userId)
        {
            await WithConnectionAsync(async (connection, transaction) =>
            {
                const string sql = @"
IF EXISTS (SELECT 1 FROM WarrantyCreditNotes WHERE WarrantyClaimId = @WarrantyClaimId)
    UPDATE WarrantyCreditNotes SET
        UnitPrice = @UnitPrice, UnitPriceGstRateId = @UnitPriceGstRateId, UnitPriceGstPercent = @UnitPriceGstPercent,
        HandlingCharges = @HandlingCharges, HandlingGstRateId = @HandlingGstRateId, HandlingGstPercent = @HandlingGstPercent,
        LabourCharges = @LabourCharges, LabourGstRateId = @LabourGstRateId, LabourGstPercent = @LabourGstPercent,
        TotalWithoutGst = @TotalWithoutGst, TotalWithGst = @TotalWithGst,
        ModifiedBy = @UserId, ModifiedDate = SYSUTCDATETIME()
    WHERE WarrantyClaimId = @WarrantyClaimId;
ELSE
    INSERT INTO WarrantyCreditNotes (
        WarrantyClaimId, UnitPrice, UnitPriceGstRateId, UnitPriceGstPercent,
        HandlingCharges, HandlingGstRateId, HandlingGstPercent,
        LabourCharges, LabourGstRateId, LabourGstPercent,
        TotalWithoutGst, TotalWithGst, CreatedBy, ModifiedBy)
    VALUES (
        @WarrantyClaimId, @UnitPrice, @UnitPriceGstRateId, @UnitPriceGstPercent,
        @HandlingCharges, @HandlingGstRateId, @HandlingGstPercent,
        @LabourCharges, @LabourGstRateId, @LabourGstPercent,
        @TotalWithoutGst, @TotalWithGst, @UserId, @UserId);";
                await connection.ExecuteAsync(sql, new
                {
                    creditNote.WarrantyClaimId,
                    creditNote.UnitPrice,
                    creditNote.UnitPriceGstRateId,
                    creditNote.UnitPriceGstPercent,
                    creditNote.HandlingCharges,
                    creditNote.HandlingGstRateId,
                    creditNote.HandlingGstPercent,
                    creditNote.LabourCharges,
                    creditNote.LabourGstRateId,
                    creditNote.LabourGstPercent,
                    creditNote.TotalWithoutGst,
                    creditNote.TotalWithGst,
                    UserId = userId
                }, transaction);
                return 0;
            });
        }
    }
}

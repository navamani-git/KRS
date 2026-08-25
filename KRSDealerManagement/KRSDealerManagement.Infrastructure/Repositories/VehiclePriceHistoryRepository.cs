using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps app entity fields to live DB columns.
    /// Live schema uses PriceMonth/PriceYear/ChangedBy; Month/Year/CreatedBy are computed.
    /// </summary>
    public class VehiclePriceHistoryRepository : Repository<VehiclePriceHistory>
    {
        public VehiclePriceHistoryRepository(ApplicationDbContext context)
            : base(context, "VehiclePriceHistory", "PriceHistoryId") { }

        public override async Task<int> AddAsync(VehiclePriceHistory entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
INSERT INTO VehiclePriceHistory (
    ModelId, ColorId, VehicleId,
    Price, PriceMonth, PriceYear,
    EffectiveFrom, EffectiveTo,
    Notes, ChangeReason,
    ChangedBy, ChangedDate,
    ModifiedBy, ModifiedDate
)
VALUES (
    @ModelId, @ColorId, @VehicleId,
    @Price, @Month, @Year,
    @EffectiveFrom, @EffectiveTo,
    @Notes, @Notes,
    @CreatedBy, @CreatedDate,
    @ModifiedBy, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        public override async Task<bool> UpdateAsync(VehiclePriceHistory entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
UPDATE VehiclePriceHistory
SET Price = @Price,
    PriceMonth = @Month,
    PriceYear = @Year,
    EffectiveFrom = @EffectiveFrom,
    EffectiveTo = @EffectiveTo,
    Notes = @Notes,
    ChangeReason = @Notes,
    ModifiedBy = @ModifiedBy,
    ModifiedDate = @ModifiedDate,
    ChangedBy = ISNULL(@ModifiedBy, ChangedBy),
    ChangedDate = @ModifiedDate
WHERE PriceHistoryId = @PriceHistoryId";

            var rows = await connection.ExecuteAsync(sql, entity);
            return rows > 0;
        }

        public override async Task<IEnumerable<VehiclePriceHistory>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
SELECT
    PriceHistoryId,
    ModelId,
    ColorId,
    VehicleId,
    Price,
    PriceMonth AS Month,
    PriceYear AS Year,
    ISNULL(EffectiveFrom, DATEFROMPARTS(PriceYear, PriceMonth, 1)) AS EffectiveFrom,
    ISNULL(EffectiveTo, EOMONTH(ISNULL(EffectiveFrom, DATEFROMPARTS(PriceYear, PriceMonth, 1)))) AS EffectiveTo,
    Notes,
    ChangedBy AS CreatedBy,
    CreatedDate,
    ModifiedBy,
    ISNULL(ModifiedDate, CreatedDate) AS ModifiedDate
FROM VehiclePriceHistory";

            return await connection.QueryAsync<VehiclePriceHistory>(sql);
        }

        public override async Task<VehiclePriceHistory> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
SELECT
    PriceHistoryId,
    ModelId,
    ColorId,
    VehicleId,
    Price,
    PriceMonth AS Month,
    PriceYear AS Year,
    ISNULL(EffectiveFrom, DATEFROMPARTS(PriceYear, PriceMonth, 1)) AS EffectiveFrom,
    ISNULL(EffectiveTo, EOMONTH(ISNULL(EffectiveFrom, DATEFROMPARTS(PriceYear, PriceMonth, 1)))) AS EffectiveTo,
    Notes,
    ChangedBy AS CreatedBy,
    CreatedDate,
    ModifiedBy,
    ISNULL(ModifiedDate, CreatedDate) AS ModifiedDate
FROM VehiclePriceHistory
WHERE PriceHistoryId = @Id";

            return await connection.QueryFirstOrDefaultAsync<VehiclePriceHistory>(sql, new { Id = id });
        }
    }
}

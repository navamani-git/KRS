using Dapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Infrastructure.Data;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Maps app PurchaseOrder entity to live PurchaseOrders schema.
    /// Writable: PurchaseOrderId, OrderNumber, SubdealerId, AccountId, TotalAmount,
    ///           PurchaseOrderStatus, VehicleCount, RejectionReason, RequestedDate,
    ///           ApprovedBy, ApprovedDate, ModifiedDate, SubdealerNotes.
    /// Computed: OrderId, TotalQuantity, Status, AdminNotes, CreatedDate.
    /// </summary>
    public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
    {
        public PurchaseOrderRepository(ApplicationDbContext context)
            : base(context, "PurchaseOrders", "PurchaseOrderId") { }

        public override async Task<int> AddAsync(PurchaseOrder entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
INSERT INTO PurchaseOrders (
    OrderNumber, SubdealerId, AccountId,
    TotalAmount, VehicleCount, PurchaseOrderStatus,
    RejectionReason, SubdealerNotes, CreatedByDealer,
    RequestedDate, ApprovedBy, ApprovedDate, ModifiedDate
)
VALUES (
    @OrderNumber, @SubdealerId, @AccountId,
    @TotalAmount, @TotalQuantity, @PurchaseOrderStatus,
    @AdminNotes, @SubdealerNotes, @CreatedByDealer,
    @CreatedDate, @ApprovedBy, @ApprovedDate, @ModifiedDate
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                entity.OrderNumber,
                entity.SubdealerId,
                entity.AccountId,
                entity.TotalAmount,
                entity.TotalQuantity,
                PurchaseOrderStatus = MapPoStatus(entity.Status),
                entity.CreatedByDealer,
                entity.AdminNotes,
                entity.SubdealerNotes,
                entity.CreatedDate,
                entity.ApprovedBy,
                entity.ApprovedDate,
                entity.ModifiedDate
            });
        }

        public override async Task<bool> UpdateAsync(PurchaseOrder entity)
        {
            using var connection = _context.GetConnection();
            connection.Open();

            const string sql = @"
UPDATE PurchaseOrders
SET OrderNumber = @OrderNumber,
    SubdealerId = @SubdealerId,
    AccountId = @AccountId,
    TotalAmount = @TotalAmount,
    VehicleCount = @TotalQuantity,
    PurchaseOrderStatus = @PurchaseOrderStatus,
    ApprovedAmount = @ApprovedAmount,
    ApprovedVehicleCount = @ApprovedVehicleCount,
    RejectionReason = @AdminNotes,
    SubdealerNotes = @SubdealerNotes,
    CreatedByDealer = @CreatedByDealer,
    ApprovedBy = @ApprovedBy,
    ApprovedDate = @ApprovedDate,
    ModifiedDate = @ModifiedDate
WHERE PurchaseOrderId = @OrderId";

            var rows = await connection.ExecuteAsync(sql, new
            {
                entity.OrderId,
                entity.OrderNumber,
                entity.SubdealerId,
                entity.AccountId,
                entity.TotalAmount,
                entity.TotalQuantity,
                PurchaseOrderStatus = MapPoStatus(entity.Status),
                entity.ApprovedAmount,
                entity.ApprovedVehicleCount,
                entity.AdminNotes,
                entity.SubdealerNotes,
                entity.CreatedByDealer,
                entity.ApprovedBy,
                entity.ApprovedDate,
                entity.ModifiedDate
            });
            return rows > 0;
        }

        public override async Task<PurchaseOrder> GetByIdAsync(int id)
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryFirstOrDefaultAsync<PurchaseOrder>(SelectSql + " WHERE PurchaseOrderId = @Id", new { Id = id });
        }

        public override async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            using var connection = _context.GetConnection();
            connection.Open();
            return await connection.QueryAsync<PurchaseOrder>(SelectSql);
        }

        private const string SelectSql = @"
SELECT
    PurchaseOrderId AS OrderId,
    OrderNumber,
    SubdealerId,
    ISNULL(AccountId, 0) AS AccountId,
    TotalAmount,
    ISNULL(ApprovedAmount, 0) AS ApprovedAmount,
    ISNULL(ApprovedVehicleCount, 0) AS ApprovedVehicleCount,
    VehicleCount AS TotalQuantity,
    ISNULL(PurchaseOrderStatus, 1) - 1 AS Status,
    ISNULL(CreatedByDealer, 0) AS CreatedByDealer,
    RejectionReason AS AdminNotes,
    SubdealerNotes,
    ApprovedBy,
    ApprovedDate,
    CAST(NULL AS datetime) AS DeliveryDate,
    RequestedDate AS CreatedDate,
    ModifiedDate
FROM PurchaseOrders";

        public async Task<PurchaseOrderListPage> QueryPageAsync(PurchaseOrderListCriteria criteria)
        {
            criteria ??= new PurchaseOrderListCriteria();
            var take = criteria.Take > 0 ? criteria.Take : 50;
            var skip = criteria.Skip > 0 ? criteria.Skip : 0;
            var dealershipSql = criteria.ApplyDealershipFilter
                ? @"AND po.SubdealerId IN (
                        SELECT DISTINCT uor.SubDealerId
                        FROM UserOrgRoles uor
                        WHERE uor.IsActive = 1
                          AND uor.SubDealerId IS NOT NULL
                          AND uor.DealershipId IN @DealershipIds)"
                : "";

            var pageSql = criteria.IncludeAll
                ? ""
                : "OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            var orderBy = OrderBySql(criteria.SortColumn, criteria.SortDescending);
            var cte = $@"
WITH shaped AS (
    SELECT
        po.PurchaseOrderId AS OrderId,
        ISNULL(po.AccountId, 0) AS AccountId,
        ISNULL(acc.AccountName, N'Unknown') AS AccountName,
        po.SubdealerId,
        CASE
            WHEN sd.SubDealerId IS NULL THEN N'Subdealer #' + CAST(po.SubdealerId AS varchar(20))
            WHEN NULLIF(LTRIM(RTRIM(sd.Location)), N'') IS NULL THEN sd.SubDealerName
            ELSE sd.SubDealerName + N' (' + sd.Location + N')'
        END AS SubdealerName,
        ISNULL(po.OrderNumber, N'') AS OrderNumber,
        ISNULL(po.VehicleCount, 0) AS TotalQuantity,
        po.TotalAmount,
        CASE
            WHEN ISNULL(items.PendingItemCount, 0) > 0 THEN 1
            WHEN ISNULL(veh.VehicleCount, 0) = 0 THEN 3
            WHEN veh.RejectedCount = veh.VehicleCount THEN 3
            WHEN ISNULL(veh.SubmittedCount, 0) > 0 THEN 1
            ELSE ISNULL(veh.MinStatus, 3)
        END AS Status,
        CAST(ISNULL(po.CreatedByDealer, 0) AS bit) AS CreatedByDealer,
        ISNULL(items.PendingItemCount, 0) AS PendingItemCount,
        ISNULL(items.ApprovedItemCount, 0) AS ApprovedItemCount,
        po.RejectionReason AS AdminNotes,
        po.SubdealerNotes,
        po.ApprovedBy,
        po.ApprovedDate,
        COALESCE(items.LastItemDate, po.ApprovedDate) AS LastAllocatedDate,
        po.RequestedDate AS CreatedDate,
        ISNULL(po.ModifiedDate, po.RequestedDate) AS ModifiedDate
    FROM PurchaseOrders po
    LEFT JOIN SubDealers sd ON sd.SubDealerId = po.SubdealerId
    LEFT JOIN SubdealerAccounts acc ON acc.AccountId = po.AccountId
    OUTER APPLY (
        SELECT
            SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS PendingItemCount,
            SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS ApprovedItemCount,
            MAX(COALESCE(ApprovedDate, RejectedDate)) AS LastItemDate
        FROM PurchaseOrderItems
        WHERE PurchaseOrderId = po.PurchaseOrderId
    ) items
    OUTER APPLY (
        SELECT
            COUNT(*) AS VehicleCount,
            SUM(CASE WHEN VehicleStatus = 3 THEN 1 ELSE 0 END) AS RejectedCount,
            SUM(CASE WHEN VehicleStatus = 1 THEN 1 ELSE 0 END) AS SubmittedCount,
            MIN(VehicleStatus) AS MinStatus
        FROM SubdealerVehicles
        WHERE PurchaseOrderId = po.PurchaseOrderId
          AND VehicleStatus <> 15
    ) veh
    WHERE (@OrderId IS NULL OR po.PurchaseOrderId = @OrderId)
      AND (@OrderId IS NOT NULL OR @FromDate IS NULL OR po.RequestedDate >= @FromDate)
      AND (@OrderId IS NOT NULL OR @ToExclusive IS NULL OR po.RequestedDate < @ToExclusive)
      AND (@SubdealerId IS NULL OR po.SubdealerId = @SubdealerId)
      AND (@AccountId IS NULL OR po.AccountId = @AccountId)
      AND (@Search IS NULL OR po.OrderNumber LIKE @Search)
      {dealershipSql}
),
named AS (
    SELECT s.*,
        st.StatusName,
        st.BadgeClass AS StatusBadgeClass,
        CASE
            WHEN s.PendingItemCount > 0 AND s.ApprovedItemCount > 0
                THEN N'Partial (' + CAST(s.PendingItemCount AS varchar(20)) + N' pending)'
            ELSE ISNULL(st.StatusName, N'')
        END AS StatusDisplay
    FROM shaped s
    OUTER APPLY (
        SELECT TOP 1 StatusName, BadgeClass
        FROM StatusLookups
        WHERE Category = N'VEHICLE' AND StatusValue = s.Status
        ORDER BY StatusLookupId
    ) st
),
filtered AS (
    SELECT *
    FROM named
    WHERE (@Status IS NULL OR Status = @Status)
      AND (@OrderNumberLike IS NULL OR OrderNumber LIKE @OrderNumberLike)
      AND (@SubdealerLike IS NULL OR SubdealerName LIKE @SubdealerLike)
      AND (@Created IS NULL OR CONVERT(date, CreatedDate) = @Created)
      AND (@Allocated IS NULL OR (LastAllocatedDate IS NOT NULL AND CONVERT(date, LastAllocatedDate) = @Allocated))
      AND (@QtyLike IS NULL OR CONVERT(varchar(20), TotalQuantity) LIKE @QtyLike)
      AND (@PendingLike IS NULL OR CONVERT(varchar(20), PendingItemCount) LIKE @PendingLike)
      AND (@AmountLike IS NULL OR FORMAT(TotalAmount, 'N2') LIKE @AmountLike)
      AND (@StatusLike IS NULL OR StatusDisplay LIKE @StatusLike)
      AND (@NotesLike IS NULL OR ISNULL(AdminNotes, SubdealerNotes) LIKE @NotesLike)
)";

            var sql = cte + @"
SELECT COUNT(*) AS TotalItems,
       ISNULL(SUM(CASE WHEN PendingItemCount > 0 THEN 1 ELSE 0 END), 0) AS PendingCount
FROM filtered;
" + cte + $@"
SELECT *
FROM filtered
ORDER BY {orderBy}
{pageSql};";

            var args = new
            {
                criteria.OrderId,
                criteria.SubdealerId,
                criteria.AccountId,
                criteria.Status,
                criteria.FromDate,
                ToExclusive = criteria.ToDateExclusive,
                Search = LikeContains(criteria.SearchTerm),
                DealershipIds = criteria.DealershipIds?.ToArray() ?? Array.Empty<int>(),
                OrderNumberLike = LikeContains(criteria.OrderNumber),
                SubdealerLike = LikeContains(criteria.Subdealer),
                criteria.Created,
                criteria.Allocated,
                QtyLike = LikeContains(criteria.Qty),
                PendingLike = LikeContains(criteria.Pending),
                AmountLike = LikeContains(criteria.Amount),
                StatusLike = LikeContains(criteria.StatusText),
                NotesLike = LikeContains(criteria.Notes),
                Skip = skip,
                Take = take
            };

            return await WithConnectionAsync(async (connection, transaction) =>
            {
                using var multi = await connection.QueryMultipleAsync(sql, args, transaction);
                var counts = await multi.ReadFirstAsync<PurchaseOrderListCount>();
                var rows = (await multi.ReadAsync<PurchaseOrderListRow>()).ToList();
                return new PurchaseOrderListPage
                {
                    TotalItems = counts.TotalItems,
                    PendingCount = counts.PendingCount,
                    Items = rows
                };
            });
        }

        private static string OrderBySql(string? column, bool descending)
        {
            var dir = descending ? "DESC" : "ASC";
            return (column ?? "").Trim().ToLowerInvariant() switch
            {
                "ordernumber" => $"OrderNumber {dir}, OrderId DESC",
                "subdealer" => $"SubdealerName {dir}, OrderId DESC",
                "qty" => $"TotalQuantity {dir}, OrderId DESC",
                "pending" => $"PendingItemCount {dir}, OrderId DESC",
                "amount" => $"TotalAmount {dir}, OrderId DESC",
                "status" => $"StatusDisplay {dir}, OrderId DESC",
                "notes" => $"ISNULL(AdminNotes, SubdealerNotes) {dir}, OrderId DESC",
                "allocated" => $"LastAllocatedDate {dir}, OrderId DESC",
                "approved" => $"ApprovedDate {dir}, OrderId DESC",
                "created" => $"CreatedDate {dir}, OrderId DESC",
                _ => "CreatedDate DESC, OrderId DESC"
            };
        }

        private static string? LikeContains(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim()
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");
            return "%" + trimmed + "%";
        }

        private sealed class PurchaseOrderListCount
        {
            public int TotalItems { get; set; }
            public int PendingCount { get; set; }
        }

        private static int MapPoStatus(int vehicleStatus) => vehicleStatus switch
        {
            UnifiedVehicleStatus.Submitted => 1,
            UnifiedVehicleStatus.ApprovedByDealer => 2,
            UnifiedVehicleStatus.RejectedByDealer => 3,
            _ => 1
        };
    }
}

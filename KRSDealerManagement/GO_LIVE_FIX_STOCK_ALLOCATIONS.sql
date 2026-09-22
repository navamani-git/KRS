/*
  Fix go-live stock issues on live DB (run once after GO_LIVE_IMPORT_STOCK_FROM_REPORT.sql)

  1. Release 35 Salem FG vehicles wrongly allocated to "Test Sub Dealer"
  2. Backfill VehicleMasterHistory + SubdealerVehicleHistory for chassis history screen

  Safe to re-run (idempotent).
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @CreatedBy INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE LOWER(Username) = N'admin' ORDER BY UserId);
IF @CreatedBy IS NULL SET @CreatedBy = 1;

DECLARE @TestSubDealerId INT = (
    SELECT TOP 1 sd.SubDealerId
    FROM dbo.SubDealers sd
    WHERE sd.IsActive = 1
      AND (
            LOWER(LTRIM(RTRIM(sd.SubDealerCode))) LIKE N'test%'
         OR LOWER(LTRIM(RTRIM(sd.SubDealerName))) LIKE N'test %'
      )
    ORDER BY sd.SubDealerId
);

IF @TestSubDealerId IS NOT NULL
BEGIN
    DECLARE @released INT = 0;

    UPDATE vm
    SET IsAllocated = 0,
        ModifiedBy = @CreatedBy,
        ModifiedDate = SYSUTCDATETIME()
    FROM dbo.VehicleMasters vm
    INNER JOIN dbo.SubdealerVehicles sv ON sv.VehicleMasterId = vm.VehicleMasterId
    WHERE sv.SubdealerId = @TestSubDealerId
      AND vm.IsAllocated = 1;

    SET @released = @@ROWCOUNT;

    DELETE sv
    FROM dbo.SubdealerVehicles sv
    WHERE sv.SubdealerId = @TestSubDealerId;

    PRINT CONCAT('Released ', @released, ' vehicle(s) from Test Sub Dealer (SubDealerId=', @TestSubDealerId, ').');
END
ELSE
    PRINT 'Test Sub Dealer not found — skip release step.';

INSERT INTO dbo.VehicleMasterHistory (VehicleMasterId, Action, Remarks, DetailsJson, UserId, CreatedDate)
SELECT
    vm.VehicleMasterId,
    N'GoLiveOpeningStock',
    CONCAT(N'Opening stock from ', LTRIM(RTRIM(vm.Remarks)), N' report.'),
    NULL,
    @CreatedBy,
    COALESCE(vm.CreatedDate, SYSUTCDATETIME())
FROM dbo.VehicleMasters vm
WHERE vm.Remarks IS NOT NULL
  AND LTRIM(RTRIM(vm.Remarks)) <> N''
  AND NOT EXISTS (
    SELECT 1 FROM dbo.VehicleMasterHistory h
    WHERE h.VehicleMasterId = vm.VehicleMasterId
      AND h.Action = N'GoLiveOpeningStock'
);

INSERT INTO dbo.SubdealerVehicleHistory (SubdealerVehicleId, Action, Remarks, DetailsJson, UserId, CreatedDate)
SELECT
    sv.SubdealerVehicleId,
    N'GoLiveAllocated',
    CONCAT(N'Opening stock allocated from ', LTRIM(RTRIM(COALESCE(sv.Remarks, vm.Remarks))), N' report.'),
    NULL,
    @CreatedBy,
    COALESCE(sv.AllocatedDate, sv.CreatedDate, SYSUTCDATETIME())
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
WHERE sv.SubdealerId IS NOT NULL
  AND sv.SubdealerId > 0
  AND NOT EXISTS (
    SELECT 1 FROM dbo.SubdealerVehicleHistory h
    WHERE h.SubdealerVehicleId = sv.SubdealerVehicleId
      AND h.Action = N'GoLiveAllocated'
);

COMMIT TRAN;

PRINT '--- Post-fix summary ---';
SELECT
    SUM(CASE WHEN vm.IsAllocated = 1 THEN 1 ELSE 0 END) AS Allocated,
    SUM(CASE WHEN vm.IsAllocated = 0 THEN 1 ELSE 0 END) AS DealerUnallocated,
    COUNT(*) AS Total
FROM dbo.VehicleMasters vm;

SELECT sd.SubDealerName, sd.Location, COUNT(*) AS Cnt
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.SubDealers sd ON sd.SubDealerId = sv.SubdealerId
GROUP BY sd.SubDealerName, sd.Location
ORDER BY Cnt DESC, sd.SubDealerName;

GO

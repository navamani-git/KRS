/*
  Lifecycle superseded status (15) — audit history rows hidden from operational lists.
  Run once on production after deploying application code that uses UnifiedVehicleStatus.LifecycleSuperseded.

  - Adds VEHICLE status lookup 15
  - Supersedes duplicate active rows per VehicleMasterId (keeps latest ModifiedDate / SubdealerVehicleId)
*/
SET NOCOUNT ON;

DECLARE @cat NVARCHAR(20) = N'VEHICLE';
DECLARE @superseded INT = 15;

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = @cat AND StatusValue = @superseded)
BEGIN
    INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive)
    VALUES (@cat, @superseded, N'LIFECYCLE_SUPERSEDED', N'Superseded (History)', N'bg-secondary', 145, 1);
    PRINT 'Inserted StatusLookups VEHICLE status 15 (Lifecycle Superseded).';
END
ELSE
    PRINT 'StatusLookups VEHICLE status 15 already exists — skipped insert.';
GO

/* Backfill: when multiple non-superseded rows exist for one chassis master, keep only the latest. */
;WITH Ranked AS (
    SELECT
        sv.SubdealerVehicleId,
        sv.VehicleMasterId,
        sv.VehicleStatus,
        ROW_NUMBER() OVER (
            PARTITION BY sv.VehicleMasterId
            ORDER BY sv.ModifiedDate DESC, sv.SubdealerVehicleId DESC
        ) AS rn
    FROM dbo.SubdealerVehicles sv
    WHERE sv.VehicleMasterId > 0
      AND sv.VehicleStatus <> 15
)
UPDATE sv
SET
    sv.VehicleStatus = 15,
    sv.ModifiedDate = SYSUTCDATETIME()
FROM dbo.SubdealerVehicles sv
INNER JOIN Ranked r ON r.SubdealerVehicleId = sv.SubdealerVehicleId
WHERE r.rn > 1;

PRINT 'Superseded older duplicate SubdealerVehicles rows: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO

/* Optional audit note for superseded rows (skip if history table missing). */
IF OBJECT_ID('dbo.SubdealerVehicleHistory', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.SubdealerVehicleHistory (SubdealerVehicleId, Action, Remarks, UserId, CreatedDate)
    SELECT
        sv.SubdealerVehicleId,
        N'LifecycleSuperseded',
        N'Superseded by ADD_LIFECYCLE_SUPERSEDED.sql — prior allocation cycle retained for audit.',
        NULL,
        SYSUTCDATETIME()
    FROM dbo.SubdealerVehicles sv
    WHERE sv.VehicleStatus = 15
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.SubdealerVehicleHistory h
          WHERE h.SubdealerVehicleId = sv.SubdealerVehicleId
            AND h.Action = N'LifecycleSuperseded'
      );

    PRINT 'Added SubdealerVehicleHistory audit rows: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END
GO

/* Report masters that still have more than one active row (should be zero). */
SELECT
    sv.VehicleMasterId,
    vm.ChassisNumber,
    COUNT(*) AS ActiveRowCount
FROM dbo.SubdealerVehicles sv
INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = sv.VehicleMasterId
WHERE sv.VehicleStatus <> 15
  AND sv.VehicleMasterId > 0
GROUP BY sv.VehicleMasterId, vm.ChassisNumber
HAVING COUNT(*) > 1;
GO

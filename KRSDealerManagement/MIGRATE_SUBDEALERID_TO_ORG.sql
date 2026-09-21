/*
  Migrate operational SubdealerId columns from login UserId → business SubDealerId (org).
  Accounts/wallet tables keep UserId — wallet stays on primary login.

  Run once on each environment AFTER backup.
  Idempotent where possible.

  NOTE: No single wrapping transaction — avoids Msg 3930 when a FK step fails mid-run.
*/
SET NOCOUNT ON;

-- Clear any doomed transaction from a prior failed run
IF @@TRANCOUNT > 0
BEGIN
    PRINT 'Rolling back open transaction from prior run...';
    ROLLBACK TRAN;
END

-- Map login UserId → SubDealerId (prefer primary assignment)
IF OBJECT_ID('tempdb..#UserToOrg') IS NOT NULL DROP TABLE #UserToOrg;

SELECT
    uor.UserId,
    uor.SubDealerId,
    ROW_NUMBER() OVER (
        PARTITION BY uor.UserId
        ORDER BY uor.IsPrimary DESC, uor.IsActive DESC, uor.UserOrgRoleId
    ) AS rn
INTO #UserToOrg
FROM dbo.UserOrgRoles uor
WHERE uor.SubDealerId IS NOT NULL;

DELETE FROM #UserToOrg WHERE rn <> 1;

CREATE UNIQUE CLUSTERED INDEX IX_UserToOrg ON #UserToOrg(UserId);

DECLARE @map TABLE (UserId INT NOT NULL PRIMARY KEY, SubDealerId INT NOT NULL);

INSERT INTO @map (UserId, SubDealerId)
SELECT UserId, SubDealerId FROM #UserToOrg;

PRINT 'Mapped ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' login users to SubDealer orgs.';

/* ── Drop FK on SubdealerId → Users if present ── */
DECLARE @sql NVARCHAR(MAX);

DECLARE fk CURSOR LOCAL FAST_FORWARD FOR
SELECT
    'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id))
    + ' DROP CONSTRAINT ' + QUOTENAME(name)
FROM sys.foreign_keys
WHERE referenced_object_id = OBJECT_ID('dbo.Users')
  AND parent_object_id IN (
      OBJECT_ID('dbo.SubdealerVehicles'),
      OBJECT_ID('dbo.Vehicles'),
      OBJECT_ID('dbo.PurchaseOrders'),
      OBJECT_ID('dbo.Commissions'),
      OBJECT_ID('dbo.Payments'),
      OBJECT_ID('dbo.WarrantyClaims')
  )
  AND EXISTS (
      SELECT 1 FROM sys.foreign_key_columns fkc
      INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
      WHERE fkc.constraint_object_id = foreign_keys.object_id
        AND c.name = 'SubdealerId'
  );

OPEN fk;
FETCH NEXT FROM fk INTO @sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT @sql;
    EXEC sp_executesql @sql;
    FETCH NEXT FROM fk INTO @sql;
END
CLOSE fk;
DEALLOCATE fk;

/* ── Backfill: UserId → SubDealerId where not already migrated ── */
IF OBJECT_ID('dbo.SubdealerVehicles') IS NOT NULL
BEGIN
    UPDATE v SET v.SubdealerId = m.SubDealerId
    FROM dbo.SubdealerVehicles v
    INNER JOIN @map m ON m.UserId = v.SubdealerId
    WHERE v.SubdealerId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = v.SubdealerId);
    PRINT 'Updated SubdealerVehicles: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END
ELSE IF OBJECT_ID('dbo.Vehicles') IS NOT NULL
BEGIN
    UPDATE v SET v.SubdealerId = m.SubDealerId
    FROM dbo.Vehicles v
    INNER JOIN @map m ON m.UserId = v.SubdealerId
    WHERE v.SubdealerId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = v.SubdealerId);
    PRINT 'Updated Vehicles: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

IF OBJECT_ID('dbo.VehicleBookings') IS NOT NULL
BEGIN
    UPDATE b SET b.SubdealerId = m.SubDealerId
    FROM dbo.VehicleBookings b
    INNER JOIN @map m ON m.UserId = b.SubdealerId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = b.SubdealerId);
    PRINT 'Updated VehicleBookings: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

IF OBJECT_ID('dbo.PurchaseOrders') IS NOT NULL
BEGIN
    UPDATE o SET o.SubdealerId = m.SubDealerId
    FROM dbo.PurchaseOrders o
    INNER JOIN @map m ON m.UserId = o.SubdealerId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = o.SubdealerId);
    PRINT 'Updated PurchaseOrders: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

IF OBJECT_ID('dbo.Commissions') IS NOT NULL
BEGIN
    UPDATE c SET c.SubdealerId = m.SubDealerId
    FROM dbo.Commissions c
    INNER JOIN @map m ON m.UserId = c.SubdealerId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = c.SubdealerId);
    PRINT 'Updated Commissions: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

IF OBJECT_ID('dbo.Payments') IS NOT NULL
BEGIN
    UPDATE p SET p.SubdealerId = m.SubDealerId
    FROM dbo.Payments p
    INNER JOIN @map m ON m.UserId = p.SubdealerId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = p.SubdealerId);
    PRINT 'Updated Payments: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

IF OBJECT_ID('dbo.WarrantyClaims') IS NOT NULL
BEGIN
    UPDATE w SET w.SubdealerId = m.SubDealerId
    FROM dbo.WarrantyClaims w
    INNER JOIN @map m ON m.UserId = w.SubdealerId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = w.SubdealerId);
    PRINT 'Updated WarrantyClaims: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

/* ── Orphan check before adding FK (values not in SubDealers) ── */
IF OBJECT_ID('dbo.SubdealerVehicles') IS NOT NULL
BEGIN
    SELECT TOP 20 v.SubdealerVehicleId, v.SubdealerId AS OrphanSubdealerId
    FROM dbo.SubdealerVehicles v
    WHERE v.SubdealerId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = v.SubdealerId);
END

/* ── Add FK to SubDealers (skip on failure — data may need manual fix) ── */
DECLARE @addFk TABLE (TableName SYSNAME, FkName SYSNAME);

INSERT INTO @addFk VALUES
    ('dbo.SubdealerVehicles', 'FK_SubdealerVehicles_SubDealerOrg'),
    ('dbo.Vehicles', 'FK_Vehicles_SubDealerOrg'),
    ('dbo.VehicleBookings', 'FK_VehicleBookings_SubDealerOrg'),
    ('dbo.PurchaseOrders', 'FK_PurchaseOrders_SubDealerOrg'),
    ('dbo.Commissions', 'FK_Commissions_SubDealerOrg'),
    ('dbo.Payments', 'FK_Payments_SubDealerOrg'),
    ('dbo.WarrantyClaims', 'FK_WarrantyClaims_SubDealerOrg');

DECLARE @t SYSNAME, @fk SYSNAME;

DECLARE addfk CURSOR LOCAL FAST_FORWARD FOR
SELECT TableName, FkName FROM @addFk;

OPEN addfk;
FETCH NEXT FROM addfk INTO @t, @fk;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(@t) IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @fk)
    BEGIN
        SET @sql = N'ALTER TABLE ' + @t + N'
            WITH NOCHECK
            ADD CONSTRAINT ' + QUOTENAME(@fk) + N'
            FOREIGN KEY (SubdealerId) REFERENCES dbo.SubDealers(SubDealerId);';
        BEGIN TRY
            EXEC sp_executesql @sql;
            PRINT 'Added ' + @fk + ' on ' + @t;
        END TRY
        BEGIN CATCH
            PRINT 'Skipped FK ' + @fk + ' on ' + @t + ': ' + ERROR_MESSAGE();
        END CATCH
    END
    FETCH NEXT FROM addfk INTO @t, @fk;
END

CLOSE addfk;
DEALLOCATE addfk;

PRINT 'MIGRATE_SUBDEALERID_TO_ORG completed.';
GO

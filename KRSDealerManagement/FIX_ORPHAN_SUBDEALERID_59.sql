/*
  Fix orphan SubdealerId = 59 on warranty-only test vehicle (SubdealerVehicleId 11).
  User 59 (ramesh / Salem staff) has no SubDealerId in UserOrgRoles — vehicle belongs on Salem Own Showroom org.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @OwnShowroomOrgId INT =
(
    SELECT TOP 1 sd.SubDealerId
    FROM dbo.SubDealers sd
    INNER JOIN dbo.VehicleMasters vm ON vm.VehicleMasterId = 385
    WHERE sd.DealershipId = vm.DealershipId
      AND sd.OwnShowroom = 1
      AND sd.IsActive = 1
    ORDER BY sd.SubDealerId
);

IF @OwnShowroomOrgId IS NULL
BEGIN
    RAISERROR('Salem Own Showroom org not found for VehicleMasterId 385.', 16, 1);
    RETURN;
END

PRINT 'Target org SubDealerId = ' + CAST(@OwnShowroomOrgId AS VARCHAR(10));

UPDATE dbo.SubdealerVehicles
SET SubdealerId = @OwnShowroomOrgId
WHERE SubdealerVehicleId = 11
  AND SubdealerId = 59;

PRINT 'Updated SubdealerVehicles: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

IF OBJECT_ID('dbo.VehicleBookings') IS NOT NULL
BEGIN
    UPDATE dbo.VehicleBookings
    SET SubdealerId = @OwnShowroomOrgId
    WHERE SubdealerVehicleId = 11
      AND SubdealerId = 59;

    PRINT 'Updated VehicleBookings: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
END

-- Verify no orphans remain
SELECT v.SubdealerVehicleId, v.SubdealerId AS OrphanSubdealerId
FROM dbo.SubdealerVehicles v
WHERE v.SubdealerId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.SubDealers sd WHERE sd.SubDealerId = v.SubdealerId);

-- Re-validate FK if it exists (was added WITH NOCHECK during migration)
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SubdealerVehicles_SubDealerOrg')
BEGIN
    BEGIN TRY
        ALTER TABLE dbo.SubdealerVehicles WITH CHECK CHECK CONSTRAINT FK_SubdealerVehicles_SubDealerOrg;
        PRINT 'FK_SubdealerVehicles_SubDealerOrg validated (WITH CHECK).';
    END TRY
    BEGIN CATCH
        PRINT 'FK validation failed: ' + ERROR_MESSAGE();
    END CATCH
END

PRINT 'FIX_ORPHAN_SUBDEALERID_59 completed.';
GO

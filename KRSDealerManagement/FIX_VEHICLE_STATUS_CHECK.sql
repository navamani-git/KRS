/*
  Fix CK_VehicleStatus — allow unified lifecycle statuses 1-14.
  Old constraint only allowed 1-4, which blocked booking (status 7).
*/
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_VehicleStatus')
    ALTER TABLE dbo.Vehicles DROP CONSTRAINT CK_VehicleStatus;
GO

ALTER TABLE dbo.Vehicles ADD CONSTRAINT CK_VehicleStatus
    CHECK (VehicleStatus BETWEEN 1 AND 14);
GO

-- Sync vehicles that have bookings but stale status (e.g. booking created before fix)
UPDATE v
SET v.VehicleStatus = b.BookingStatus,
    v.ModifiedDate = GETUTCDATE()
FROM dbo.Vehicles v
INNER JOIN dbo.VehicleBookings b ON b.VehicleId = v.VehicleId
WHERE v.VehicleStatus < 7 AND b.BookingStatus BETWEEN 7 AND 14;
GO

PRINT '=== CK_VehicleStatus updated (1-14) ===';
SELECT VehicleId, VehicleStatus FROM dbo.Vehicles WHERE VehicleId IN (
    SELECT VehicleId FROM dbo.VehicleBookings
);
GO

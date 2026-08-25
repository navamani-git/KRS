-- Sync vehicle + booking status from milestone dates (booking phase vehicles only)
DECLARE @Expected TABLE (VehicleId INT PRIMARY KEY, ExpectedStatus INT);

INSERT INTO @Expected (VehicleId, ExpectedStatus)
SELECT v.VehicleId,
    CASE
        WHEN NULLIF(LTRIM(RTRIM(b.SubsidyId)), '') IS NOT NULL THEN 13
        WHEN b.RegistrationDate IS NOT NULL THEN 12
        WHEN b.AgentDate IS NOT NULL THEN 11
        WHEN b.InsuranceDate IS NOT NULL THEN 10
        WHEN b.InvoiceDate IS NOT NULL THEN 9
        WHEN b.PaperReceivedDate IS NOT NULL THEN 8
        ELSE 7
    END
FROM dbo.Vehicles v
INNER JOIN dbo.VehicleBookings b ON b.VehicleId = v.VehicleId
WHERE v.VehicleStatus BETWEEN 7 AND 13;

UPDATE v
SET v.VehicleStatus = e.ExpectedStatus,
    v.ModifiedDate = GETUTCDATE()
FROM dbo.Vehicles v
INNER JOIN @Expected e ON e.VehicleId = v.VehicleId
WHERE v.VehicleStatus <> e.ExpectedStatus;

UPDATE b
SET b.BookingStatus = e.ExpectedStatus,
    b.ModifiedDate = GETUTCDATE()
FROM dbo.VehicleBookings b
INNER JOIN @Expected e ON e.VehicleId = b.VehicleId
WHERE b.BookingStatus <> e.ExpectedStatus;
GO

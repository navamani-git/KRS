/*
  LOCAL DB ONLY — unified vehicle status flow (14 statuses).
*/
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_VehicleStatus')
    ALTER TABLE dbo.Vehicles DROP CONSTRAINT CK_VehicleStatus;
GO

ALTER TABLE dbo.Vehicles ADD CONSTRAINT CK_VehicleStatus
    CHECK (VehicleStatus BETWEEN 1 AND 14);
GO

DELETE FROM dbo.StatusLookups
WHERE Category IN (N'ORDER', N'ORDER_ITEM', N'RETURN', N'BOOKING', N'VEHICLE');
GO

INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder, IsActive) VALUES
 (N'VEHICLE',  1, N'SUBMITTED',           N'Submitted',           N'bg-warning text-dark', 10, 1),
 (N'VEHICLE',  2, N'APPROVED_BY_DEALER',  N'Approved By Dealer',  N'bg-success', 20, 1),
 (N'VEHICLE',  3, N'REJECTED_BY_DEALER',  N'Rejected By Dealer',  N'bg-danger', 30, 1),
 (N'VEHICLE',  4, N'RETURN_REQUESTED',    N'Return Requested',    N'bg-warning text-dark', 40, 1),
 (N'VEHICLE',  5, N'RETURN_APPROVED',     N'Return Approved',     N'bg-info', 50, 1),
 (N'VEHICLE',  6, N'RETURN_CANCELLED',    N'Return Cancelled',    N'bg-secondary', 60, 1),
 (N'VEHICLE',  7, N'BOOKED_TO_CUSTOMER',  N'Booked to Customer',  N'bg-primary', 70, 1),
 (N'VEHICLE',  8, N'PAPER_RECEIVED',      N'Paper Received',      N'bg-info', 80, 1),
 (N'VEHICLE',  9, N'INVOICED',            N'Invoiced',            N'bg-info', 90, 1),
 (N'VEHICLE', 10, N'INSURANCE_CREATED',   N'Insurance Created',   N'bg-info', 100, 1),
 (N'VEHICLE', 11, N'RTO_REQUESTED',       N'RTO Requested',       N'bg-warning text-dark', 110, 1),
 (N'VEHICLE', 12, N'REGISTERED',          N'Registered',          N'bg-warning text-dark', 120, 1),
 (N'VEHICLE', 13, N'SUBSIDY_ID_CREATED',  N'Subsidy ID Created',  N'bg-warning text-dark', 130, 1),
 (N'VEHICLE', 14, N'DELIVERED',           N'Delivered',           N'bg-success', 140, 1);
GO

-- Booking rows -> vehicle status 7-14
UPDATE v SET v.VehicleStatus = 6 + b.BookingStatus
FROM dbo.Vehicles v
INNER JOIN dbo.VehicleBookings b ON b.VehicleId = v.VehicleId
WHERE b.BookingStatus BETWEEN 1 AND 8 AND v.VehicleStatus < 7;
GO

-- Placeholder chassis -> Submitted
UPDATE dbo.Vehicles SET VehicleStatus = 1
WHERE ChassisNumber LIKE N'PENDING-%' AND VehicleStatus NOT BETWEEN 1 AND 14;
GO

-- Legacy purchased / available -> Approved
UPDATE dbo.Vehicles SET VehicleStatus = 2
WHERE VehicleStatus IN (0, 1, 4) AND ChassisNumber NOT LIKE N'PENDING-%'
  AND VehicleId NOT IN (SELECT VehicleId FROM dbo.VehicleBookings);
GO

UPDATE dbo.Vehicles SET VehicleStatus = 2
WHERE VehicleStatus NOT BETWEEN 1 AND 14;
GO

IF COL_LENGTH('dbo.PurchaseOrders', 'CreatedByDealer') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD CreatedByDealer BIT NOT NULL
        CONSTRAINT DF_PurchaseOrders_CreatedByDealer DEFAULT(0);
GO

UPDATE b SET b.BookingStatus = v.VehicleStatus
FROM dbo.VehicleBookings b
INNER JOIN dbo.Vehicles v ON v.VehicleId = b.VehicleId
WHERE v.VehicleStatus >= 7;
GO

PRINT '=== Unified vehicle status applied ===';
SELECT StatusValue, StatusName FROM dbo.StatusLookups WHERE Category = N'VEHICLE' ORDER BY StatusValue;
GO

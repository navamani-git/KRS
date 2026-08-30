-- Number plate handover details on vehicle bookings
IF COL_LENGTH('dbo.VehicleBookings', 'NumberPlateReceivedBy') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD NumberPlateReceivedBy NVARCHAR(200) NULL;

PRINT 'VehicleBookings.NumberPlateReceivedBy column added.';

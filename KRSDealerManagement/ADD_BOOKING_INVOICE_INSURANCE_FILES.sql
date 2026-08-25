-- Invoice & insurance document paths on vehicle bookings (staff upload, subdealer view/download)
IF COL_LENGTH('dbo.VehicleBookings', 'InvoicePath') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD InvoicePath NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.VehicleBookings', 'InsurancePath') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD InsurancePath NVARCHAR(500) NULL;
GO

PRINT 'VehicleBookings invoice/insurance file columns added.';
GO

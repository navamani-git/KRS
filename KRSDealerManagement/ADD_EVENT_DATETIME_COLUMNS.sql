/* Store date+time for operational events (run once per database). */
IF COL_LENGTH('dbo.VehicleBookings', 'PaperReceivedDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.VehicleBookings ALTER COLUMN PaperReceivedDate DATETIME2 NULL;
    ALTER TABLE dbo.VehicleBookings ALTER COLUMN InvoiceDate DATETIME2 NULL;
    ALTER TABLE dbo.VehicleBookings ALTER COLUMN InsuranceDate DATETIME2 NULL;
    ALTER TABLE dbo.VehicleBookings ALTER COLUMN AgentDate DATETIME2 NULL;
    ALTER TABLE dbo.VehicleBookings ALTER COLUMN RegistrationDate DATETIME2 NULL;
    ALTER TABLE dbo.VehicleBookings ALTER COLUMN NumberPlateReceivedDate DATETIME2 NULL;
    PRINT 'VehicleBookings milestone columns converted to DATETIME2.';
END

IF COL_LENGTH('dbo.Vehicles', 'DeliveryDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Vehicles ALTER COLUMN DeliveryDate DATETIME2 NULL;
    PRINT 'Vehicles.DeliveryDate converted to DATETIME2.';
END

IF COL_LENGTH('dbo.VehicleMasters', 'AmpereInvoiceDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.VehicleMasters ALTER COLUMN AmpereInvoiceDate DATETIME2 NOT NULL;
    ALTER TABLE dbo.VehicleMasters ALTER COLUMN ReceivedDate DATETIME2 NOT NULL;
    PRINT 'VehicleMasters date columns converted to DATETIME2.';
END

-- Vehicle delivery date captured when subdealer marks delivered
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'DeliveryDate' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    ALTER TABLE Vehicles ADD DeliveryDate DATE NULL;
    PRINT 'Added DeliveryDate to Vehicles';
END
GO

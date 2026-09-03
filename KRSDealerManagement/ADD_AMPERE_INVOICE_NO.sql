-- Replace ManufacturingYear with AmpereInvoiceNo on dealer stock (VehicleMasters)
USE KRSDealerManagementDB;
GO

IF COL_LENGTH('VehicleMasters', 'AmpereInvoiceNo') IS NULL
BEGIN
    ALTER TABLE VehicleMasters ADD AmpereInvoiceNo NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('VehicleMasters', 'ManufacturingYear') IS NOT NULL
BEGIN
    UPDATE VehicleMasters
    SET AmpereInvoiceNo = COALESCE(NULLIF(LTRIM(RTRIM(AmpereInvoiceNo)), ''), CAST(ManufacturingYear AS NVARCHAR(50)))
    WHERE AmpereInvoiceNo IS NULL OR LTRIM(RTRIM(AmpereInvoiceNo)) = '';

    ALTER TABLE VehicleMasters DROP COLUMN ManufacturingYear;
END
GO

IF COL_LENGTH('VehicleMasters', 'AmpereInvoiceNo') IS NOT NULL
BEGIN
    UPDATE VehicleMasters SET AmpereInvoiceNo = '' WHERE AmpereInvoiceNo IS NULL;
    ALTER TABLE VehicleMasters ALTER COLUMN AmpereInvoiceNo NVARCHAR(50) NOT NULL;
END
GO

-- SO Number on warranty claims (required when applying to Ampere, before product received)
IF COL_LENGTH('dbo.WarrantyClaims', 'SoNumber') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD SoNumber NVARCHAR(50) NULL;
GO

PRINT 'WarrantyClaims.SoNumber column applied.';
GO

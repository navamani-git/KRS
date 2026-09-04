-- Warranty: custom part name when "Others" is selected
IF COL_LENGTH('dbo.WarrantyClaims', 'OtherPartName') IS NULL
    ALTER TABLE dbo.WarrantyClaims ADD OtherPartName NVARCHAR(200) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.WarrantyParts WHERE PartName = N'OTHERS')
    INSERT INTO dbo.WarrantyParts (PartName, PartCode, SortOrder, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'OTHERS', N'OTHERS', 999, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
GO

PRINT 'Warranty Others part support applied.';
GO

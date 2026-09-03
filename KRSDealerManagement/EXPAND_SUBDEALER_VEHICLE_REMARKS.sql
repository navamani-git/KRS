-- Expand SubdealerVehicles.Remarks for admin correction history (was NVARCHAR(500)).
IF COL_LENGTH('dbo.SubdealerVehicles', 'Remarks') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SubdealerVehicles ALTER COLUMN Remarks NVARCHAR(MAX) NULL;
    PRINT 'Expanded SubdealerVehicles.Remarks to NVARCHAR(MAX).';
END
GO

-- Notes column for price revision comments on allocated vehicles
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Vehicles') AND name = 'Notes')
    ALTER TABLE dbo.Vehicles ADD Notes NVARCHAR(MAX) NULL;
GO

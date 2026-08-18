-- Model-to-color mapping (run once on your database before using admin mapping screens)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'VehicleModelColors' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.VehicleModelColors (
        ModelId INT NOT NULL,
        ColorId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_VehicleModelColors_IsActive DEFAULT (1),
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_VehicleModelColors_CreatedDate DEFAULT (GETUTCDATE()),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_VehicleModelColors_ModifiedDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_VehicleModelColors PRIMARY KEY (ModelId, ColorId),
        CONSTRAINT FK_VehicleModelColors_Model FOREIGN KEY (ModelId) REFERENCES dbo.VehicleModels(ModelId),
        CONSTRAINT FK_VehicleModelColors_Color FOREIGN KEY (ColorId) REFERENCES dbo.VehicleColors(ColorId)
    );

    CREATE INDEX IX_VehicleModelColors_ModelId ON dbo.VehicleModelColors(ModelId);
    CREATE INDEX IX_VehicleModelColors_ColorId ON dbo.VehicleModelColors(ColorId);

    PRINT 'Created table VehicleModelColors.';
END
ELSE
    PRINT 'Table VehicleModelColors already exists.';

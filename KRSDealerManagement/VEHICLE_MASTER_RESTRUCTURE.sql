/*
  Vehicle Master / Subdealer Vehicle restructure
  ==============================================
  Run on LOCAL and SERVER (idempotent where possible).

  BEFORE FIRST RUN (recommended): LOCAL_RESET_UAT_OPTION_A.sql on test DBs.

  Creates:
    - VehicleMasters          (dealer inventory from Ampere)
    - SubdealerVehicles       (operational unit after allocation)
    - VehicleMasterHistory
    - SubdealerVehicleHistory

  Renames:
    - PurchaseOrderItems.VehicleId -> SubdealerVehicleId (when present)
    - VehicleBookings.VehicleId    -> SubdealerVehicleId (when present)
    - CommissionHistory.VehicleId   -> SubdealerVehicleId (when present)
    - ReturnRequests.VehicleId     -> SubdealerVehicleId (when present)

  Drops legacy Vehicles table after migrating FKs (only when empty or after reset).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ── VehicleMasters ── */
IF OBJECT_ID('dbo.VehicleMasters', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleMasters (
        VehicleMasterId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DealershipId          INT NOT NULL,
        ChassisNumber         NVARCHAR(50) NOT NULL,
        ModelId               INT NOT NULL,
        ColorId               INT NOT NULL,
        MotorNo               NVARCHAR(100) NOT NULL,
        BatteryNo             NVARCHAR(100) NOT NULL,
        ChargerNo             NVARCHAR(100) NOT NULL,
        ControllerNo          NVARCHAR(100) NOT NULL,
        ConverterNo           NVARCHAR(100) NOT NULL,
        ManufacturingYear     INT NOT NULL,
        AmpereInvoiceDate     DATE NOT NULL,
        ReceivedDate          DATE NOT NULL,
        IsAllocated           BIT NOT NULL CONSTRAINT DF_VehicleMasters_IsAllocated DEFAULT(0),
        Remarks               NVARCHAR(500) NULL,
        CreatedBy             INT NOT NULL,
        CreatedDate           DATETIME2 NOT NULL CONSTRAINT DF_VehicleMasters_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedBy            INT NULL,
        ModifiedDate          DATETIME2 NOT NULL CONSTRAINT DF_VehicleMasters_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT UQ_VehicleMasters_Chassis UNIQUE (ChassisNumber),
        CONSTRAINT FK_VehicleMasters_Dealership FOREIGN KEY (DealershipId) REFERENCES dbo.Dealerships(DealershipId),
        CONSTRAINT FK_VehicleMasters_Model FOREIGN KEY (ModelId) REFERENCES dbo.VehicleModels(ModelId),
        CONSTRAINT FK_VehicleMasters_Color FOREIGN KEY (ColorId) REFERENCES dbo.VehicleColors(ColorId)
    );
    CREATE INDEX IX_VehicleMasters_Dealership_Allocated ON dbo.VehicleMasters(DealershipId, IsAllocated);
    CREATE INDEX IX_VehicleMasters_Model_Color ON dbo.VehicleMasters(ModelId, ColorId);
    PRINT 'Created VehicleMasters.';
END
GO

/* ── SubdealerVehicles ── */
IF OBJECT_ID('dbo.SubdealerVehicles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubdealerVehicles (
        SubdealerVehicleId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VehicleMasterId       INT NOT NULL,
        SubdealerId           INT NULL,
        PurchaseOrderId       INT NULL,
        VehicleStatus         INT NOT NULL CONSTRAINT DF_SubdealerVehicles_Status DEFAULT(2),
        CurrentPrice          DECIMAL(18,2) NOT NULL CONSTRAINT DF_SubdealerVehicles_Price DEFAULT(0),
        OriginalPrice         DECIMAL(18,2) NOT NULL CONSTRAINT DF_SubdealerVehicles_OrigPrice DEFAULT(0),
        RegistrationNumber    NVARCHAR(50) NULL,
        DeliveryDate          DATE NULL,
        AllocatedDate         DATETIME2 NULL,
        AllocatedBy           INT NULL,
        Remarks               NVARCHAR(500) NULL,
        CreatedBy             INT NOT NULL,
        CreatedDate           DATETIME2 NOT NULL CONSTRAINT DF_SubdealerVehicles_Created DEFAULT(SYSUTCDATETIME()),
        ModifiedBy            INT NULL,
        ModifiedDate          DATETIME2 NOT NULL CONSTRAINT DF_SubdealerVehicles_Modified DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_SubdealerVehicles_Master FOREIGN KEY (VehicleMasterId) REFERENCES dbo.VehicleMasters(VehicleMasterId),
        CONSTRAINT FK_SubdealerVehicles_Subdealer FOREIGN KEY (SubdealerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_SubdealerVehicles_Order FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrders(PurchaseOrderId)
    );
    CREATE INDEX IX_SubdealerVehicles_Master ON dbo.SubdealerVehicles(VehicleMasterId);
    CREATE INDEX IX_SubdealerVehicles_Subdealer ON dbo.SubdealerVehicles(SubdealerId);
    CREATE INDEX IX_SubdealerVehicles_Order ON dbo.SubdealerVehicles(PurchaseOrderId);
    PRINT 'Created SubdealerVehicles.';
END
GO

/* ── History tables ── */
IF OBJECT_ID('dbo.VehicleMasterHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleMasterHistory (
        VehicleMasterHistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VehicleMasterId        INT NOT NULL,
        Action                 NVARCHAR(80) NOT NULL,
        Remarks                NVARCHAR(500) NULL,
        DetailsJson            NVARCHAR(MAX) NULL,
        UserId                 INT NULL,
        CreatedDate            DATETIME2 NOT NULL CONSTRAINT DF_VehicleMasterHistory_Created DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_VehicleMasterHistory_Master FOREIGN KEY (VehicleMasterId) REFERENCES dbo.VehicleMasters(VehicleMasterId)
    );
    CREATE INDEX IX_VehicleMasterHistory_Master ON dbo.VehicleMasterHistory(VehicleMasterId, CreatedDate DESC);
    PRINT 'Created VehicleMasterHistory.';
END
GO

IF OBJECT_ID('dbo.SubdealerVehicleHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubdealerVehicleHistory (
        SubdealerVehicleHistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SubdealerVehicleId        INT NOT NULL,
        Action                    NVARCHAR(80) NOT NULL,
        Remarks                   NVARCHAR(500) NULL,
        DetailsJson               NVARCHAR(MAX) NULL,
        UserId                    INT NULL,
        CreatedDate               DATETIME2 NOT NULL CONSTRAINT DF_SubdealerVehicleHistory_Created DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_SubdealerVehicleHistory_Vehicle FOREIGN KEY (SubdealerVehicleId) REFERENCES dbo.SubdealerVehicles(SubdealerVehicleId)
    );
    CREATE INDEX IX_SubdealerVehicleHistory_Vehicle ON dbo.SubdealerVehicleHistory(SubdealerVehicleId, CreatedDate DESC);
    PRINT 'Created SubdealerVehicleHistory.';
END
GO

/* ── PurchaseOrderItems: VehicleId -> SubdealerVehicleId ── */
IF COL_LENGTH('dbo.PurchaseOrderItems', 'SubdealerVehicleId') IS NULL
   AND COL_LENGTH('dbo.PurchaseOrderItems', 'VehicleId') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.PurchaseOrderItems.VehicleId', 'SubdealerVehicleId', 'COLUMN';
    PRINT 'Renamed PurchaseOrderItems.VehicleId -> SubdealerVehicleId.';
END
GO

IF COL_LENGTH('dbo.PurchaseOrderItems', 'SubdealerVehicleId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderItems ADD SubdealerVehicleId INT NULL;
    PRINT 'Added PurchaseOrderItems.SubdealerVehicleId.';
END
GO

/* ── VehicleBookings ── */
IF COL_LENGTH('dbo.VehicleBookings', 'SubdealerVehicleId') IS NULL
   AND COL_LENGTH('dbo.VehicleBookings', 'VehicleId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VB_Vehicle')
        ALTER TABLE dbo.VehicleBookings DROP CONSTRAINT FK_VB_Vehicle;
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_VehicleBookings_Vehicle')
        ALTER TABLE dbo.VehicleBookings DROP CONSTRAINT UQ_VehicleBookings_Vehicle;

    EXEC sp_rename 'dbo.VehicleBookings.VehicleId', 'SubdealerVehicleId', 'COLUMN';
    PRINT 'Renamed VehicleBookings.VehicleId -> SubdealerVehicleId.';
END
GO

IF COL_LENGTH('dbo.VehicleBookings', 'SubdealerVehicleId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_VB_SubdealerVehicle')
BEGIN
    ALTER TABLE dbo.VehicleBookings WITH NOCHECK
        ADD CONSTRAINT FK_VB_SubdealerVehicle FOREIGN KEY (SubdealerVehicleId)
        REFERENCES dbo.SubdealerVehicles(SubdealerVehicleId);
    ALTER TABLE dbo.VehicleBookings ADD CONSTRAINT UQ_VehicleBookings_SubdealerVehicle UNIQUE (SubdealerVehicleId);
END
GO

/* ── CommissionHistory ── */
IF COL_LENGTH('dbo.CommissionHistory', 'SubdealerVehicleId') IS NULL
   AND COL_LENGTH('dbo.CommissionHistory', 'VehicleId') IS NOT NULL
BEGIN
    DECLARE @fkComm NVARCHAR(200);
    DECLARE @sqlComm NVARCHAR(500);
    SELECT @fkComm = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.CommissionHistory') AND c.name = 'VehicleId';
    IF @fkComm IS NOT NULL
    BEGIN
        SET @sqlComm = N'ALTER TABLE dbo.CommissionHistory DROP CONSTRAINT ' + QUOTENAME(@fkComm);
        EXEC sp_executesql @sqlComm;
    END
    EXEC sp_rename 'dbo.CommissionHistory.VehicleId', 'SubdealerVehicleId', 'COLUMN';
    PRINT 'Renamed CommissionHistory.VehicleId -> SubdealerVehicleId.';
END
GO

/* ── ReturnRequests ── */
IF COL_LENGTH('dbo.ReturnRequests', 'SubdealerVehicleId') IS NULL
   AND COL_LENGTH('dbo.ReturnRequests', 'VehicleId') IS NOT NULL
BEGIN
    DECLARE @fkRet NVARCHAR(200);
    DECLARE @sqlRet NVARCHAR(500);
    SELECT @fkRet = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.ReturnRequests') AND c.name = 'VehicleId';
    IF @fkRet IS NOT NULL
    BEGIN
        SET @sqlRet = N'ALTER TABLE dbo.ReturnRequests DROP CONSTRAINT ' + QUOTENAME(@fkRet);
        EXEC sp_executesql @sqlRet;
    END
    EXEC sp_rename 'dbo.ReturnRequests.VehicleId', 'SubdealerVehicleId', 'COLUMN';
    PRINT 'Renamed ReturnRequests.VehicleId -> SubdealerVehicleId.';
END
GO

/* ── VehiclePriceHistory: VehicleId -> SubdealerVehicleId ── */
IF COL_LENGTH('dbo.VehiclePriceHistory', 'SubdealerVehicleId') IS NULL
   AND COL_LENGTH('dbo.VehiclePriceHistory', 'VehicleId') IS NOT NULL
BEGIN
    DECLARE @fkVph NVARCHAR(200);
    DECLARE @sqlVph NVARCHAR(500);
    SELECT @fkVph = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.VehiclePriceHistory') AND c.name = 'VehicleId';
    IF @fkVph IS NOT NULL
    BEGIN
        SET @sqlVph = N'ALTER TABLE dbo.VehiclePriceHistory DROP CONSTRAINT ' + QUOTENAME(@fkVph);
        EXEC sp_executesql @sqlVph;
    END
    EXEC sp_rename 'dbo.VehiclePriceHistory.VehicleId', 'SubdealerVehicleId', 'COLUMN';
    PRINT 'Renamed VehiclePriceHistory.VehicleId -> SubdealerVehicleId.';
END
GO

IF COL_LENGTH('dbo.VehiclePriceHistory', 'SubdealerVehicleId') IS NULL
BEGIN
    ALTER TABLE dbo.VehiclePriceHistory ADD SubdealerVehicleId INT NULL;
    PRINT 'Added VehiclePriceHistory.SubdealerVehicleId.';
END
GO

/* ── NumberPlateReceivedBy (if not yet applied) ── */
IF COL_LENGTH('dbo.VehicleBookings', 'NumberPlateReceivedBy') IS NULL
    ALTER TABLE dbo.VehicleBookings ADD NumberPlateReceivedBy NVARCHAR(200) NULL;
GO

/* ── Drop legacy Vehicles table when SubdealerVehicles exists ── */
IF OBJECT_ID('dbo.SubdealerVehicles', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Vehicles', 'U') IS NOT NULL
BEGIN
    DECLARE @dropVehicles NVARCHAR(MAX) = N'';
    SELECT @dropVehicles = @dropVehicles + N'ALTER TABLE dbo.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID('dbo.Vehicles');
    IF LEN(@dropVehicles) > 0 EXEC sp_executesql @dropVehicles;
    DROP TABLE dbo.Vehicles;
    PRINT 'Dropped legacy Vehicles table.';
END
GO

PRINT 'VEHICLE_MASTER_RESTRUCTURE.sql completed.';
PRINT 'Run LOCAL_RESET_UAT_OPTION_A.sql before testing if legacy Vehicles data exists.';

/*
  GST rate master + per-screen default preselect
  Run on dealer DB after deploy.
*/
IF OBJECT_ID(N'dbo.GstRates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GstRates (
        GstRateId    INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        RatePercent  DECIMAL(5, 2)  NOT NULL,
        SortOrder    INT            NOT NULL CONSTRAINT DF_GstRates_SortOrder DEFAULT (0),
        IsActive     BIT            NOT NULL CONSTRAINT DF_GstRates_IsActive DEFAULT (1),
        CreatedDate  DATETIME2(7)   NOT NULL CONSTRAINT DF_GstRates_Created DEFAULT (SYSUTCDATETIME()),
        ModifiedDate DATETIME2(7)   NOT NULL CONSTRAINT DF_GstRates_Modified DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_GstRates_RatePercent UNIQUE (RatePercent)
    );
END

IF OBJECT_ID(N'dbo.GstScreenDefaults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GstScreenDefaults (
        ScreenKey    NVARCHAR(100)  NOT NULL PRIMARY KEY,
        GstRateId      INT            NOT NULL,
        ModifiedDate   DATETIME2(7)   NOT NULL CONSTRAINT DF_GstScreenDefaults_Modified DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_GstScreenDefaults_GstRates FOREIGN KEY (GstRateId) REFERENCES dbo.GstRates (GstRateId)
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.GstRates WHERE RatePercent = 10)
    INSERT INTO dbo.GstRates (RatePercent, SortOrder) VALUES (10, 10);
IF NOT EXISTS (SELECT 1 FROM dbo.GstRates WHERE RatePercent = 15)
    INSERT INTO dbo.GstRates (RatePercent, SortOrder) VALUES (15, 20);
IF NOT EXISTS (SELECT 1 FROM dbo.GstRates WHERE RatePercent = 20)
    INSERT INTO dbo.GstRates (RatePercent, SortOrder) VALUES (20, 30);

DELETE FROM dbo.GstScreenDefaults WHERE ScreenKey IN (N'screen_a', N'screen_b', N'screen_c');

DECLARE @SystemAdmin INT = (SELECT RoleId FROM dbo.Roles WHERE RoleCode = N'SYSTEM_ADMIN');
IF @SystemAdmin IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId = @SystemAdmin AND MenuKey = N'admin_gst_rates')
    INSERT INTO dbo.RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
    VALUES (@SystemAdmin, N'admin_gst_rates', N'GST Rates', 1, 63);

GO

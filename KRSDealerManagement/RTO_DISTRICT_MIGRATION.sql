/*
  RTO District + Location hierarchy.
  Run on LOCAL and SERVER.

  - RtoDistrictMasters: Salem, Erode, Namakkal, Salem South, etc.
  - RtoLocationMasters.RtoDistrictId: Mettur, Omalur under Salem, etc.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.RtoDistrictMasters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RtoDistrictMasters (
        RtoDistrictId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DistrictName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RtoDistrictMasters_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_RtoDistrictMasters_CreatedDate DEFAULT (SYSUTCDATETIME()),
        ModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_RtoDistrictMasters_ModifiedDate DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_RtoDistrictMasters_DistrictName
        ON dbo.RtoDistrictMasters (DistrictName);
END
GO

IF COL_LENGTH('dbo.RtoLocationMasters', 'RtoDistrictId') IS NULL
BEGIN
    ALTER TABLE dbo.RtoLocationMasters ADD RtoDistrictId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_RtoLocationMasters_RtoDistrictMasters')
BEGIN
    ALTER TABLE dbo.RtoLocationMasters WITH CHECK
        ADD CONSTRAINT FK_RtoLocationMasters_RtoDistrictMasters
        FOREIGN KEY (RtoDistrictId) REFERENCES dbo.RtoDistrictMasters (RtoDistrictId);
END
GO

-- Assign legacy locations without a district to a default district.
IF EXISTS (SELECT 1 FROM dbo.RtoLocationMasters WHERE RtoDistrictId IS NULL)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.RtoDistrictMasters WHERE DistrictName = N'General')
        INSERT INTO dbo.RtoDistrictMasters (DistrictName) VALUES (N'General');

    DECLARE @GeneralDistrictId INT = (
        SELECT TOP 1 RtoDistrictId FROM dbo.RtoDistrictMasters WHERE DistrictName = N'General');

    UPDATE dbo.RtoLocationMasters
    SET RtoDistrictId = @GeneralDistrictId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE RtoDistrictId IS NULL;
END
GO

-- Optional sample master data (only when district table is empty).
IF NOT EXISTS (SELECT 1 FROM dbo.RtoDistrictMasters)
BEGIN
    INSERT INTO dbo.RtoDistrictMasters (DistrictName) VALUES
        (N'Salem'), (N'Erode'), (N'Namakkal'), (N'Salem South');

    DECLARE @SalemId INT = (SELECT RtoDistrictId FROM dbo.RtoDistrictMasters WHERE DistrictName = N'Salem');
    DECLARE @ErodeId INT = (SELECT RtoDistrictId FROM dbo.RtoDistrictMasters WHERE DistrictName = N'Erode');

    INSERT INTO dbo.RtoLocationMasters (LocationName, RtoDistrictId, IsActive)
    SELECT v.LocationName, v.RtoDistrictId, 1
    FROM (VALUES
        (N'Mettur', @SalemId),
        (N'Omalur', @SalemId),
        (N'Salem City', @SalemId),
        (N'Erode City', @ErodeId)
    ) AS v(LocationName, RtoDistrictId)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.RtoLocationMasters existing
        WHERE UPPER(LTRIM(RTRIM(existing.LocationName))) = UPPER(LTRIM(RTRIM(v.LocationName))));
END
GO

PRINT 'RTO district migration complete.';
GO

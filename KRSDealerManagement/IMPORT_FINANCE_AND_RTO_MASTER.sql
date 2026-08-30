/*
  Import finance names + RTO districts/locations (deduplicated).
  Run on LOCAL and SERVER after RTO_DISTRICT_MIGRATION.sql.

  - Finance: merges 3 lists from requirements (case-insensitive dedupe).
  - RTO: maps each office to Tamil Nadu revenue district (TN Transport STA, 2024).
    Salem zone: Mettur, Omalur, Attur, Valappadi, Salem E/W, Sankagiri -> Salem;
    Salem (South) -> Salem South district.
    Erode zone: Erode, Gobi, Bhavani, Perundurai -> Erode.
    Namakkal zone offices -> Namakkal. Tiruchi zone -> Tiruchirappalli. etc.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ─── Finance names ───────────────────────────────────────────────────────────
DECLARE @FinanceNames TABLE (FinanceName NVARCHAR(150) NOT NULL PRIMARY KEY);

INSERT INTO @FinanceNames (FinanceName) VALUES
 (N'CASH'),
 (N'AKARAM AUTO FINANCIERS'),
 (N'ANNAMAR ENTERPRISES'),
 (N'AXIS BANK LTD'),
 (N'BAJAJ FINANCE LIMITED'),
 (N'Bank of Baroda'),
 (N'Bank of India'),
 (N'Canara Bank'),
 (N'CHOLAMANDALAM INVESTMENT AND FINANCE CO LTD'),
 (N'Deccan Finance Limited'),
 (N'GREAVES FINANCE LIMITED'),
 (N'HDB FINANCIAL SERVICES LTD'),
 (N'HDFC BANK LTD'),
 (N'ICICI BANK LTD'),
 (N'IDFC FIRST BANK LIMITED'),
 (N'Indian Bank'),
 (N'INDIAN OVERSEAS BANK'),
 (N'Indusind Bank Limited'),
 (N'Jana Small Finance Bank Limited'),
 (N'L&T Finance Ltd.'),
 (N'LIC Of India'),
 (N'MANAPURAM FINANCE LIMITED'),
 (N'Muthoot Capital Services Ltd'),
 (N'RBL Bank Ltd'),
 (N'Salem District Central Cooperative Bank'),
 (N'Shriram Finance Limited'),
 (N'SREE VENKATESWARA FINANCE'),
 (N'TATA CAPITAL LIMITED'),
 (N'The Karur Vysya Bank Ltd'),
 (N'Udhaya Finance'),
 (N'UNION BANK OF INDIA'),
 (N'Wheelsemi Pvt Ltd'),
 (N'Ujjivan Small Finance Bank'),
 (N'ACCRETIVE CLEANTECH FINANCE PRIVATE LIMITED'),
 (N'Sri Karuppana Finance Corps'),
 (N'CORE FINANCE'),
 (N'Hero FinCorp Ltd'),
 (N'Sri Jeyam Finance'),
 (N'SRI SENDHUR MURUGAN FINANCE'),
 (N'TAMILNAD MERCANTILE BANK LTD'),
 (N'Yogakshemam Loans Limited'),
 (N'TAMILNADU GRAMA BANK'),
 (N'DHANASRI FINANCE'),
 (N'Transport Finance');

INSERT INTO dbo.FinanceNames (FinanceName, IsActive, CreatedDate, ModifiedDate)
SELECT f.FinanceName, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @FinanceNames f
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.FinanceNames existing
    WHERE UPPER(LTRIM(RTRIM(existing.FinanceName))) = UPPER(LTRIM(RTRIM(f.FinanceName))));

PRINT CONCAT('Finance names added: ', @@ROWCOUNT);

-- ─── RTO districts ─────────────────────────────────────────────────────────
DECLARE @Districts TABLE (DistrictName NVARCHAR(100) NOT NULL PRIMARY KEY);

INSERT INTO @Districts (DistrictName) VALUES
 (N'Salem'),
 (N'Salem South'),
 (N'Erode'),
 (N'Namakkal'),
 (N'Dharmapuri'),
 (N'Karur'),
 (N'Tiruchirappalli'),
 (N'Perambalur'),
 (N'Kallakurichi'),
 (N'Tirupathur'),
 (N'Dindigul'),
 (N'Tiruppur'),
 (N'Cuddalore');

INSERT INTO dbo.RtoDistrictMasters (DistrictName, IsActive, CreatedDate, ModifiedDate)
SELECT d.DistrictName, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @Districts d
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RtoDistrictMasters existing
    WHERE UPPER(LTRIM(RTRIM(existing.DistrictName))) = UPPER(LTRIM(RTRIM(d.DistrictName))));

PRINT CONCAT('RTO districts added: ', @@ROWCOUNT);

-- ─── RTO locations → district mapping ────────────────────────────────────────
DECLARE @RtoLocations TABLE (
    LocationName NVARCHAR(100) NOT NULL PRIMARY KEY,
    DistrictName NVARCHAR(100) NOT NULL
);

INSERT INTO @RtoLocations (LocationName, DistrictName) VALUES
 (N'ARAVAKURICHI', N'Karur'),
 (N'ATTUR', N'Salem'),
 (N'AMBUR', N'Tirupathur'),
 (N'BHAVANI', N'Erode'),
 (N'DHARMAPURI', N'Dharmapuri'),
 (N'DHARAPURAM', N'Tiruppur'),
 (N'DINDUKAL', N'Dindigul'),
 (N'ERODE', N'Erode'),
 (N'ERODE (WEST)', N'Erode'),
 (N'HARUR', N'Dharmapuri'),
 (N'GOBI', N'Erode'),
 (N'KALLAKURICHI', N'Kallakurichi'),
 (N'KARUR', N'Karur'),
 (N'KULITHALI', N'Karur'),
 (N'KUMARAPALAYAM', N'Namakkal'),
 (N'MANAPARAI', N'Tiruchirappalli'),
 (N'MANMANGALAM', N'Karur'),
 (N'METTUR', N'Salem'),
 (N'MUSURI', N'Tiruchirappalli'),
 (N'NAMAKKAL (NORTH)', N'Namakkal'),
 (N'NAMAKKAL (SOUTH)', N'Namakkal'),
 (N'OMALUR', N'Salem'),
 (N'PARAMATHI VELLUR', N'Namakkal'),
 (N'PERAMBALUR', N'Perambalur'),
 (N'PERUNDURAI', N'Erode'),
 (N'RASIPURAM', N'Namakkal'),
 (N'SALEM (EAST)', N'Salem'),
 (N'SALEM (SOUTH)', N'Salem South'),
 (N'SALEM (WEST)', N'Salem'),
 (N'SANKAGIRI', N'Salem'),
 (N'SRIRANGAM', N'Tiruchirappalli'),
 (N'THURAIYUR', N'Tiruchirappalli'),
 (N'TIRUCHENGODE', N'Namakkal'),
 (N'TIRUCHI(EAST)', N'Tiruchirappalli'),
 (N'TIRUPATHUR', N'Tirupathur'),
 (N'TIRUCHI', N'Tiruchirappalli'),
 (N'VALAPPADI', N'Salem'),
 (N'VIRUDHACHALAM', N'Cuddalore');

-- Insert new locations
INSERT INTO dbo.RtoLocationMasters (LocationName, RtoDistrictId, IsActive, CreatedDate, ModifiedDate)
SELECT r.LocationName, d.RtoDistrictId, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @RtoLocations r
INNER JOIN dbo.RtoDistrictMasters d
    ON UPPER(LTRIM(RTRIM(d.DistrictName))) = UPPER(LTRIM(RTRIM(r.DistrictName)))
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RtoLocationMasters existing
    WHERE UPPER(LTRIM(RTRIM(existing.LocationName))) = UPPER(LTRIM(RTRIM(r.LocationName))));

PRINT CONCAT('RTO locations added: ', @@ROWCOUNT);

-- Fix district mapping on existing locations (same name, wrong/missing district)
UPDATE loc
SET loc.RtoDistrictId = d.RtoDistrictId,
    loc.ModifiedDate = SYSUTCDATETIME()
FROM dbo.RtoLocationMasters loc
INNER JOIN @RtoLocations r
    ON UPPER(LTRIM(RTRIM(loc.LocationName))) = UPPER(LTRIM(RTRIM(r.LocationName)))
INNER JOIN dbo.RtoDistrictMasters d
    ON UPPER(LTRIM(RTRIM(d.DistrictName))) = UPPER(LTRIM(RTRIM(r.DistrictName)))
WHERE loc.RtoDistrictId IS NULL OR loc.RtoDistrictId <> d.RtoDistrictId;

PRINT CONCAT('RTO locations re-mapped: ', @@ROWCOUNT);

-- Alias: THIRUPATHUR = TIRUPATHUR (same RTO office, Tirupathur district)
DECLARE @TirupathurDistrictId INT = (
    SELECT TOP 1 RtoDistrictId FROM dbo.RtoDistrictMasters
    WHERE UPPER(LTRIM(RTRIM(DistrictName))) = N'TIRUPATHUR');

IF @TirupathurDistrictId IS NOT NULL
BEGIN
    UPDATE dbo.RtoLocationMasters
    SET RtoDistrictId = @TirupathurDistrictId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE UPPER(LTRIM(RTRIM(LocationName))) = N'THIRUPATHUR'
      AND (RtoDistrictId IS NULL OR RtoDistrictId <> @TirupathurDistrictId);
    PRINT CONCAT('THIRUPATHUR alias re-mapped: ', @@ROWCOUNT);
END

-- Deactivate legacy placeholder locations superseded by canonical RTO offices
UPDATE dbo.RtoLocationMasters
SET IsActive = 0,
    ModifiedDate = SYSUTCDATETIME()
WHERE IsActive = 1
  AND UPPER(LTRIM(RTRIM(LocationName))) IN (N'SALEM', N'SALEM CITY', N'ERODE CITY')
  AND NOT EXISTS (
      SELECT 1 FROM @RtoLocations r
      WHERE UPPER(LTRIM(RTRIM(r.LocationName))) = UPPER(LTRIM(RTRIM(dbo.RtoLocationMasters.LocationName))));

PRINT CONCAT('Legacy RTO locations deactivated: ', @@ROWCOUNT);

-- Summary
SELECT N'FinanceNames' AS [Table], COUNT(*) AS [Rows] FROM dbo.FinanceNames WHERE IsActive = 1
UNION ALL
SELECT N'RtoDistrictMasters', COUNT(*) FROM dbo.RtoDistrictMasters WHERE IsActive = 1
UNION ALL
SELECT N'RtoLocationMasters', COUNT(*) FROM dbo.RtoLocationMasters WHERE IsActive = 1
ORDER BY [Table];
GO

PRINT 'IMPORT_FINANCE_AND_RTO_MASTER complete.';
GO

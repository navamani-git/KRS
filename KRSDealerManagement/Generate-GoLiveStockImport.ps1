# Generates GO_LIVE_IMPORT_STOCK_FROM_REPORT.sql from Ampere stock Excel.
# Usage:
#   .\Generate-GoLiveStockImport.ps1
#   .\Generate-GoLiveStockImport.ps1 -ExcelPath "D:\KRSGIT\KRS\Vehicle Current Stock Report_21-09-2026.xlsx"

param(
    [string]$ExcelPath = "D:\KRSGIT\KRS\Vehicle Current Stock Report_21-09-2026.xlsx",
    [string]$OutputSql = "D:\KRSGIT\KRS\KRSDealerManagement\GO_LIVE_IMPORT_STOCK_FROM_REPORT.sql"
)

function Escape-Sql([string]$s) {
    if ($null -eq $s) { return "NULL" }
    $t = $s.Trim().Replace("'", "''")
    if ($t.Length -eq 0) { return "NULL" }
    return "N'$t'"
}

function Parse-Date([string]$raw) {
    if ([string]::IsNullOrWhiteSpace($raw)) { return "CAST(GETDATE() AS DATE)" }
    $formats = @('dd-MM-yyyy', 'dd/MM/yyyy', 'yyyy-MM-dd', 'M/d/yyyy')
    foreach ($f in $formats) {
        try {
            $dt = [datetime]::ParseExact($raw.Trim(), $f, $null)
            return "CAST('$($dt.ToString('yyyy-MM-dd'))' AS DATE)"
        } catch {}
    }
    try {
        $dt = [datetime]::Parse($raw)
        return "CAST('$($dt.ToString('yyyy-MM-dd'))' AS DATE)"
    } catch {
        return "CAST(GETDATE() AS DATE)"
    }
}

if (-not (Test-Path $ExcelPath)) {
    throw "Excel not found: $ExcelPath"
}

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$wb = $excel.Workbooks.Open((Resolve-Path $ExcelPath).Path)
$ws = $wb.Worksheets.Item(1)
$rows = $ws.UsedRange.Rows.Count

$values = New-Object System.Collections.Generic.List[string]
for ($r = 2; $r -le $rows; $r++) {
    $branch = $ws.Cells.Item($r, 2).Text.Trim()
    $location = $ws.Cells.Item($r, 3).Text.Trim()
    $model = $ws.Cells.Item($r, 4).Text.Trim()
    $color = $ws.Cells.Item($r, 5).Text.Trim()
    $chassis = $ws.Cells.Item($r, 6).Text.Trim().ToUpperInvariant()
    $motor = $ws.Cells.Item($r, 7).Text.Trim()
    $battery = $ws.Cells.Item($r, 8).Text.Trim()
    $converter = $ws.Cells.Item($r, 9).Text.Trim()
    $charger = $ws.Cells.Item($r, 10).Text.Trim()
    $controller = $ws.Cells.Item($r, 11).Text.Trim()
    $mfg = $ws.Cells.Item($r, 12).Text.Trim()

    if ([string]::IsNullOrWhiteSpace($chassis)) { continue }

    $line = "(" +
        "$(Escape-Sql $branch), $(Escape-Sql $location), $(Escape-Sql $model), $(Escape-Sql $color), " +
        "$(Escape-Sql $chassis), $(Escape-Sql $motor), $(Escape-Sql $battery), " +
        "$(Escape-Sql $converter), $(Escape-Sql $charger), $(Escape-Sql $controller), " +
        "$(Parse-Date $mfg))"
    $values.Add($line)
}

$wb.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($ws) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($wb) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

$header = @'
/*
  GO LIVE - import opening stock from Ampere report
  ===============================================
  Source: Vehicle Current Stock Report (21-09-2026)
  Generated: {0}

  Branch mapping:
    SLM Sales Branch -> SALEM
    NKL Sales Branch -> NAMAKKAL
    KRR Sales Branch -> KARUR

  Location column -> SubDealers.Location via exact normalized match + #LocationAlias.
  Test / own-showroom subdealers are excluded. No fuzzy name matching.
  If no subdealer match: chassis stays in dealer unallocated stock (IsAllocated = 0).

  Matched rows: VehicleMasters + SubdealerVehicles (status ApprovedByDealer = 2).
  Run AFTER GO_LIVE_RESET_TRANSACTIONAL.sql
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @CreatedBy INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE LOWER(Username) = N'admin' ORDER BY UserId);
IF @CreatedBy IS NULL SET @CreatedBy = 1;
DECLARE @ApprovedStatus INT = 2; -- UnifiedVehicleStatus.ApprovedByDealer

IF OBJECT_ID('tempdb..#StockSrc') IS NOT NULL DROP TABLE #StockSrc;
CREATE TABLE #StockSrc (
    BranchName     NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    LocationName   NVARCHAR(150) COLLATE DATABASE_DEFAULT NULL,
    ModelName      NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
    ColorName      NVARCHAR(150) COLLATE DATABASE_DEFAULT NOT NULL,
    ChassisNumber  NVARCHAR(50)  COLLATE DATABASE_DEFAULT NOT NULL,
    MotorNo        NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    BatteryNo      NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    ConverterNo    NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    ChargerNo      NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    ControllerNo   NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    MfgDate        DATE NOT NULL
);

INSERT INTO #StockSrc (BranchName, LocationName, ModelName, ColorName, ChassisNumber, MotorNo, BatteryNo, ConverterNo, ChargerNo, ControllerNo, MfgDate)
VALUES
'@ -f (Get-Date -Format 'yyyy-MM-dd HH:mm')

$footer = @'

IF OBJECT_ID('tempdb..#BranchAlias') IS NOT NULL DROP TABLE #BranchAlias;
CREATE TABLE #BranchAlias (
    BranchName NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
    DealershipCode NVARCHAR(20) COLLATE DATABASE_DEFAULT NOT NULL
);
INSERT INTO #BranchAlias (BranchName, DealershipCode) VALUES
    (N'SLM Sales Branch', N'SALEM'),
    (N'NKL Sales Branch', N'NAMAKKAL'),
    (N'KRR Sales Branch', N'KARUR');

IF OBJECT_ID('tempdb..#LocationAlias') IS NOT NULL DROP TABLE #LocationAlias;
CREATE TABLE #LocationAlias (
    LocationName NVARCHAR(150) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
    MatchToken   NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL
);
INSERT INTO #LocationAlias (LocationName, MatchToken) VALUES
    (N'Ayothiapattinam FG', N'AYOTHIYAPATTINAM'),
    (N'Chinnadarapuram FG', N'CHIINA DHARAPURAM'),
    (N'Chinnappampatti FG', N'CHINNAPPAMPATTI'),
    (N'Chittoor FG', N'CHITTOR'),
    (N'Edappadi FG', N'EDAPPADI'),
    (N'Elampillai FG', N'ELAMPILLAI'),
    (N'Erumapatti FG', N'ERUMAPATTY'),
    (N'Jalakandapuram FG', N'JALAKANDAPURAM'),
    (N'K.Paramathi FG', N'PARAMATHI VELLORE'),
    (N'Kandhampalayam FG', N'KANDHAMPALAYAM'),
    (N'Komarapalayam FG', N'KOMARAPALAYAM'),
    (N'Konganapuram FG', N'KONGANAPURAM'),
    (N'Kosavampatti FG', N'KOSAVAMPATTI'),
    (N'Kulithalai FG', N'KULITHALAI'),
    (N'Kunjandiyur FG', N'KUNJANDIYUR'),
    (N'Kuranguchavadi FG', N'KURANGUCHAVADI'),
    (N'Magudanchavadi FG', N'MAGUDANCHAVADI'),
    (N'Mallasamudram FG', N'MALLASAMUTHIRAM'),
    (N'Mangalapuram FG', N'MANGALAPURAM'),
    (N'Mecheri FG', N'MECHERI'),
    (N'Mettur FG', N'METTUR'),
    (N'Mohanur FG', N'MOHANUR'),
    (N'Omalur FG', N'OMALUR'),
    (N'Pallipalayam FG', N'PALLIPALAYAM'),
    (N'Paramathi Velur FG', N'PARAMATHI VELLORE'),
    (N'Rasipuram FG', N'RASIPURAM'),
    (N'Salem FG', N'SALEM FG'),
    (N'Sankagiri FG', N'SANKAGIRI'),
    (N'Steel Plant FG', N'STEEL PLANT'),
    (N'Tharamangalam FG', N'THARAMANGALAM'),
    (N'Theevattipatti FG', N'THEEVATTIPATTI'),
    (N'Thevur FG', N'THEVUR'),
    (N'Thogaimalai FG', N'THOGAIMALAI'),
    (N'Tholasampatti FG', N'THOLASAMPATTY'),
    (N'Tiruchengode FG', N'TIRUCHENGODE'),
    (N'Trichy Road FG', N'TRICHY ROAD'),
    (N'Vaalapadi FG', N'VALAPADI'),
    (N'Veeraganur FG', N'VEERAGANUR'),
    (N'Veeranam FG', N'VEERANAM'),
    (N'Velayuthampalayam FG', N'VELAYUTHAMPALYAM'),
    (N'Attayampatti FG', N'ATTAYAMPATTI'),
    (N'Dadagapatty FG', N'DADAGAPATTY'),
    (N'Karur FG', N'KARUR FG'),
    (N'Namakkal FG', N'NAMAKKAL FG'),
    (N'Kolathur FG', N'KOLATHUR');

IF OBJECT_ID('tempdb..#ModelAlias') IS NOT NULL DROP TABLE #ModelAlias;
CREATE TABLE #ModelAlias (Src NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL, Target NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL);
INSERT INTO #ModelAlias (Src, Target) VALUES
    (N'Magnus NEO', N'Magnus Neo 12"'),
    (N'Nexus - Ex Plus', N'Nexus EX+ (Without IOT)'),
    (N'Nexus - Ex Plus IOT', N'Nexus EX+ (With IOT)'),
    (N'Nexus-EX-48V-3KW', N'Nexus EX'),
    (N'Reo VYB', N'Reo Vyb');

IF OBJECT_ID('tempdb..#ColorAlias') IS NOT NULL DROP TABLE #ColorAlias;
CREATE TABLE #ColorAlias (Src NVARCHAR(150) COLLATE DATABASE_DEFAULT NOT NULL, Target NVARCHAR(150) COLLATE DATABASE_DEFAULT NOT NULL);
INSERT INTO #ColorAlias (Src, Target) VALUES
    (N'Copper & Almond Milk', N'Cinnamon Copper & Almond Milk'),
    (N'Purple Blue', N'Purple'),
    (N'Zanskar Aqua', N'Aqua Blue'),
    (N'Glacial White', N'White'),
    (N'Single Tone Matcha Green', N'Matcha Green');

;WITH n AS (
    SELECT
        s.*,
        ba.DealershipCode,
        LOWER(LTRIM(RTRIM(s.ModelName))) COLLATE DATABASE_DEFAULT AS ModelRaw,
        LOWER(LTRIM(RTRIM(s.ColorName))) COLLATE DATABASE_DEFAULT AS ColorRaw,
        LOWER(LTRIM(RTRIM(
            CASE WHEN CHARINDEX(N'&', s.ColorName) > 0
                 THEN LEFT(s.ColorName, CHARINDEX(N'&', s.ColorName) - 1)
                 ELSE s.ColorName END))) COLLATE DATABASE_DEFAULT AS ColorHead,
        UPPER(REPLACE(REPLACE(LTRIM(RTRIM(s.LocationName)), N' FG', N''), N' ', N'')) COLLATE DATABASE_DEFAULT AS LocKey
    FROM #StockSrc s
    INNER JOIN #BranchAlias ba ON ba.BranchName = s.BranchName
)
SELECT
    n.*,
    d.DealershipId,
    rm.ModelId,
    rc.ColorId,
    sd.SubDealerId,
    sd.SubDealerName,
    COALESCE(ph.Price, 0) AS UnitPrice
INTO #Resolved
FROM n
INNER JOIN dbo.Dealerships d ON d.DealershipCode = n.DealershipCode
OUTER APPLY (
    SELECT TOP (1) x.ModelId
    FROM (
        SELECT m.ModelId, 1 AS Pri, LEN(m.ModelName) AS Ln
        FROM dbo.VehicleModels m
        WHERE LOWER(LTRIM(RTRIM(m.ModelName))) = n.ModelRaw
        UNION ALL
        SELECT m.ModelId, 2, LEN(m.ModelName)
        FROM #ModelAlias a
        INNER JOIN dbo.VehicleModels m ON LOWER(LTRIM(RTRIM(m.ModelName))) = LOWER(LTRIM(RTRIM(a.Target)))
        WHERE n.ModelRaw = LOWER(LTRIM(RTRIM(a.Src)))
           OR n.ModelRaw LIKE N'%' + LOWER(LTRIM(RTRIM(a.Src))) + N'%'
           OR LOWER(LTRIM(RTRIM(a.Src))) LIKE N'%' + n.ModelRaw + N'%'
        UNION ALL
        SELECT m.ModelId, 3, LEN(m.ModelName)
        FROM dbo.VehicleModels m
        WHERE LEN(LTRIM(RTRIM(m.ModelName))) >= 4
          AND (CHARINDEX(LOWER(LTRIM(RTRIM(m.ModelName))), n.ModelRaw) > 0
               OR CHARINDEX(n.ModelRaw, LOWER(LTRIM(RTRIM(m.ModelName)))) > 0)
    ) x
    ORDER BY x.Pri, x.Ln DESC
) rm
OUTER APPLY (
    SELECT TOP (1) x.ColorId
    FROM (
        SELECT c.ColorId, 1 AS Pri, LEN(c.ColorName) AS Ln
        FROM dbo.VehicleColors c
        WHERE LOWER(LTRIM(RTRIM(c.ColorName))) IN (n.ColorRaw, n.ColorHead)
        UNION ALL
        SELECT c.ColorId, 2, LEN(c.ColorName)
        FROM #ColorAlias a
        INNER JOIN dbo.VehicleColors c ON LOWER(LTRIM(RTRIM(c.ColorName))) = LOWER(LTRIM(RTRIM(a.Target)))
        WHERE n.ColorRaw = LOWER(LTRIM(RTRIM(a.Src)))
           OR n.ColorHead = LOWER(LTRIM(RTRIM(a.Src)))
        UNION ALL
        SELECT c.ColorId, 3, LEN(c.ColorName)
        FROM dbo.VehicleColors c
        WHERE LEN(LTRIM(RTRIM(c.ColorName))) >= 4
          AND (
                CHARINDEX(LOWER(LTRIM(RTRIM(c.ColorName))), n.ColorRaw) > 0
             OR CHARINDEX(n.ColorHead, LOWER(LTRIM(RTRIM(c.ColorName)))) > 0
             OR CHARINDEX(LOWER(LTRIM(RTRIM(c.ColorName))), n.ColorHead) > 0
          )
        UNION ALL
        SELECT c.ColorId, 4, LEN(c.ColorName)
        FROM dbo.VehicleColors c
        WHERE (n.ColorRaw LIKE N'%black%'  AND LOWER(c.ColorName) LIKE N'%black%')
           OR (n.ColorRaw LIKE N'%white%'  AND LOWER(c.ColorName) LIKE N'%white%')
           OR (n.ColorRaw LIKE N'%blue%'   AND LOWER(c.ColorName) LIKE N'%blue%')
           OR (n.ColorRaw LIKE N'%green%'  AND LOWER(c.ColorName) LIKE N'%green%')
           OR (n.ColorRaw LIKE N'%grey%'   AND LOWER(c.ColorName) LIKE N'%grey%')
           OR (n.ColorRaw LIKE N'%gray%'   AND LOWER(c.ColorName) LIKE N'%grey%')
           OR (n.ColorRaw LIKE N'%red%'    AND LOWER(c.ColorName) LIKE N'%red%')
           OR (n.ColorRaw LIKE N'%steel%'  AND LOWER(c.ColorName) LIKE N'%grey%')
           OR (n.ColorRaw LIKE N'%aqua%'   AND LOWER(c.ColorName) LIKE N'%aqua%')
           OR (n.ColorRaw LIKE N'%purple%' AND LOWER(c.ColorName) LIKE N'%purple%')
           OR (n.ColorRaw LIKE N'%copper%' AND LOWER(c.ColorName) LIKE N'%copper%')
    ) x
    ORDER BY x.Pri, x.Ln DESC
) rc
OUTER APPLY (
    SELECT TOP (1) sd.SubDealerId, sd.SubDealerName
    FROM dbo.SubDealers sd
    CROSS APPLY (
        SELECT UPPER(REPLACE(
            COALESCE(
                (SELECT la.MatchToken FROM #LocationAlias la WHERE la.LocationName = n.LocationName),
                REPLACE(LTRIM(RTRIM(n.LocationName)), N' FG', N'')
            ), N' ', N'')) AS TargetLocKey
    ) tok
    WHERE sd.DealershipId = d.DealershipId
      AND sd.IsActive = 1
      AND ISNULL(sd.OwnShowroom, 0) = 0
      AND LOWER(LTRIM(RTRIM(sd.SubDealerCode))) NOT LIKE N'test%'
      AND LOWER(LTRIM(RTRIM(sd.SubDealerName))) NOT LIKE N'test %'
      AND UPPER(REPLACE(LTRIM(RTRIM(sd.Location)), N' ', N'')) = tok.TargetLocKey
    ORDER BY sd.SubDealerId
) sd
OUTER APPLY (
    SELECT TOP (1) vph.Price
    FROM dbo.VehiclePriceHistory vph
    WHERE vph.ModelId = rm.ModelId
      AND vph.ColorId = rc.ColorId
      AND vph.EffectiveFrom <= CAST(SYSUTCDATETIME() AS DATE)
      AND (vph.EffectiveTo IS NULL OR vph.EffectiveTo >= CAST(SYSUTCDATETIME() AS DATE))
    ORDER BY vph.EffectiveFrom DESC
) ph;

IF OBJECT_ID(N'dbo.VehicleModelColors', N'U') IS NOT NULL
BEGIN
    UPDATE r
    SET ColorId = pick.ColorId
    FROM #Resolved r
    CROSS APPLY (
        SELECT TOP (1) c.ColorId
        FROM dbo.VehicleModelColors mc
        INNER JOIN dbo.VehicleColors c ON c.ColorId = mc.ColorId
        WHERE mc.ModelId = r.ModelId
          AND ISNULL(mc.IsActive, 1) = 1
        ORDER BY
            CASE
                WHEN LOWER(LTRIM(RTRIM(c.ColorName))) = r.ColorRaw THEN 0
                WHEN LOWER(LTRIM(RTRIM(c.ColorName))) = r.ColorHead THEN 1
                WHEN CHARINDEX(r.ColorHead, LOWER(c.ColorName)) > 0 THEN 2
                WHEN c.ColorId = r.ColorId THEN 3
                ELSE 4
            END,
            LEN(c.ColorName) DESC
    ) pick
    WHERE EXISTS (
        SELECT 1
        FROM dbo.VehicleModelColors mc0
        WHERE mc0.ModelId = r.ModelId
          AND ISNULL(mc0.IsActive, 1) = 1
    );
END

UPDATE r
SET UnitPrice = COALESCE(ph.Price, latest.Price, 0)
FROM #Resolved r
OUTER APPLY (
    SELECT TOP (1) vph.Price
    FROM dbo.VehiclePriceHistory vph
    WHERE vph.ModelId = r.ModelId
      AND vph.ColorId = r.ColorId
      AND vph.EffectiveFrom <= CAST(SYSUTCDATETIME() AS DATE)
      AND (vph.EffectiveTo IS NULL OR vph.EffectiveTo >= CAST(SYSUTCDATETIME() AS DATE))
    ORDER BY vph.EffectiveFrom DESC
) ph
OUTER APPLY (
    SELECT TOP (1) vph.Price
    FROM dbo.VehiclePriceHistory vph
    WHERE vph.ModelId = r.ModelId
      AND vph.ColorId = r.ColorId
    ORDER BY vph.EffectiveFrom DESC
) latest
WHERE r.UnitPrice IS NULL OR r.UnitPrice = 0;

IF EXISTS (SELECT 1 FROM #Resolved WHERE ModelId IS NULL OR ColorId IS NULL)
BEGIN
    SELECT DISTINCT ModelName, ColorName, ModelId, ColorId
    FROM #Resolved
    WHERE ModelId IS NULL OR ColorId IS NULL;
    ROLLBACK;
    THROW 50004, 'Could not map some Excel model/color names to existing masters.', 1;
END

INSERT INTO dbo.VehicleMasters (
    DealershipId, ChassisNumber, ModelId, ColorId,
    MotorNo, BatteryNo, ChargerNo, ControllerNo, ConverterNo,
    AmpereInvoiceNo, AmpereInvoiceDate, ReceivedDate,
    IsAllocated, Remarks, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
SELECT
    r.DealershipId,
    r.ChassisNumber,
    r.ModelId,
    r.ColorId,
    r.MotorNo,
    r.BatteryNo,
    r.ChargerNo,
    r.ControllerNo,
    r.ConverterNo,
    LEFT(N'STOCK-' + r.ChassisNumber, 50),
    r.MfgDate,
    r.MfgDate,
    CASE WHEN r.SubDealerId IS NULL THEN 0 ELSE 1 END,
    r.LocationName,
    @CreatedBy,
    SYSUTCDATETIME(),
    @CreatedBy,
    SYSUTCDATETIME()
FROM #Resolved r
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.VehicleMasters vm WHERE vm.ChassisNumber = r.ChassisNumber
);

INSERT INTO dbo.SubdealerVehicles (
    VehicleMasterId, SubdealerId, PurchaseOrderId, VehicleStatus,
    CurrentPrice, OriginalPrice, RegistrationNumber,
    AllocatedDate, AllocatedBy, Remarks, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
SELECT
    vm.VehicleMasterId,
    r.SubDealerId,
    NULL,
    @ApprovedStatus,
    r.UnitPrice,
    r.UnitPrice,
    N'',
    SYSUTCDATETIME(),
    @CreatedBy,
    r.LocationName,
    @CreatedBy,
    SYSUTCDATETIME(),
    @CreatedBy,
    SYSUTCDATETIME()
FROM #Resolved r
INNER JOIN dbo.VehicleMasters vm ON vm.ChassisNumber = r.ChassisNumber
WHERE r.SubDealerId IS NOT NULL
  AND NOT EXISTS (
        SELECT 1 FROM dbo.SubdealerVehicles sv WHERE sv.VehicleMasterId = vm.VehicleMasterId
    );

INSERT INTO dbo.VehicleMasterHistory (VehicleMasterId, Action, Remarks, DetailsJson, UserId, CreatedDate)
SELECT
    vm.VehicleMasterId,
    N'GoLiveOpeningStock',
    CONCAT(N'Opening stock from ', r.LocationName, N' report.'),
    NULL,
    @CreatedBy,
    SYSUTCDATETIME()
FROM #Resolved r
INNER JOIN dbo.VehicleMasters vm ON vm.ChassisNumber = r.ChassisNumber
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.VehicleMasterHistory h
    WHERE h.VehicleMasterId = vm.VehicleMasterId
      AND h.Action = N'GoLiveOpeningStock'
);

INSERT INTO dbo.SubdealerVehicleHistory (SubdealerVehicleId, Action, Remarks, DetailsJson, UserId, CreatedDate)
SELECT
    sv.SubdealerVehicleId,
    N'GoLiveAllocated',
    CONCAT(N'Opening stock allocated from ', r.LocationName, N' report.'),
    NULL,
    @CreatedBy,
    SYSUTCDATETIME()
FROM #Resolved r
INNER JOIN dbo.VehicleMasters vm ON vm.ChassisNumber = r.ChassisNumber
INNER JOIN dbo.SubdealerVehicles sv ON sv.VehicleMasterId = vm.VehicleMasterId
WHERE r.SubDealerId IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM dbo.SubdealerVehicleHistory h
    WHERE h.SubdealerVehicleId = sv.SubdealerVehicleId
      AND h.Action = N'GoLiveAllocated'
);

DECLARE @total INT = (SELECT COUNT(*) FROM #Resolved);
DECLARE @allocated INT = (SELECT COUNT(*) FROM #Resolved WHERE SubDealerId IS NOT NULL);
DECLARE @unallocated INT = @total - @allocated;

COMMIT TRAN;

PRINT CONCAT('Stock import complete. Total=', @total, ', allocated to subdealer=', @allocated, ', dealer unallocated=', @unallocated);

PRINT '--- Unallocated (no subdealer match) ---';
SELECT BranchName, LocationName, COUNT(*) AS Cnt
FROM #Resolved
WHERE SubDealerId IS NULL
GROUP BY BranchName, LocationName
ORDER BY Cnt DESC, BranchName, LocationName;

PRINT '--- Allocated by subdealer ---';
SELECT SubDealerName, COUNT(*) AS Cnt
FROM #Resolved
WHERE SubDealerId IS NOT NULL
GROUP BY SubDealerName
ORDER BY Cnt DESC, SubDealerName;

DROP TABLE #Resolved;
DROP TABLE #ColorAlias;
DROP TABLE #ModelAlias;
DROP TABLE #LocationAlias;
DROP TABLE #BranchAlias;
DROP TABLE #StockSrc;
GO
'@

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine($header)
for ($i = 0; $i -lt $values.Count; $i++) {
    $suffix = if ($i -lt $values.Count - 1) { "," } else { ";" }
    [void]$sb.AppendLine($values[$i] + $suffix)
}
[void]$sb.Append($footer)

[System.IO.File]::WriteAllText($OutputSql, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Generated $($values.Count) stock rows -> $OutputSql"

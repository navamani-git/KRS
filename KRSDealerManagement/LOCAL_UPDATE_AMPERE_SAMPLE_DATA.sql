/*
  LOCAL DB ONLY — replace non-Ampere sample vehicle/master data in an existing database.
  Run when you already have old Tata/Tesla/etc. seed data and want Ampere-only samples.

  WARNING: Deletes vehicle prices, commissions, and models/colors then reseeds Ampere data.
  Does NOT touch subdealers, orders, or payments.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- Remove old sample vehicle pricing & commissions
DELETE FROM dbo.VehiclePriceHistory;
DELETE FROM dbo.CommissionRates;

-- Remove non-Ampere or generic sample models/colors
DELETE FROM dbo.VehicleColors;
DELETE FROM dbo.VehicleModels;

DBCC CHECKIDENT ('dbo.VehicleModels', RESEED, 0);
DBCC CHECKIDENT ('dbo.VehicleColors', RESEED, 0);

INSERT INTO dbo.VehicleModels (ModelName, Description, IsActive, CreatedBy, CreatedDate, ModifiedDate)
VALUES (N'Magnus EX',   N'Ampere Magnus EX electric scooter',   1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Magnus Pro',  N'Ampere Magnus Pro electric scooter',  1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Magnus Neo',  N'Ampere Magnus Neo electric scooter',  1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Nexus EX',    N'Ampere Nexus EX electric scooter',    1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Nexus ST',    N'Ampere Nexus ST electric scooter',    1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Reo Li',      N'Ampere Reo Li electric scooter',      1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Reo Elite',   N'Ampere Reo Elite electric scooter',   1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Zeal EX',     N'Ampere Zeal EX electric scooter',     1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

INSERT INTO dbo.VehicleColors (ColorName, HexCode, IsActive, CreatedBy, CreatedDate, ModifiedDate)
VALUES (N'Pearl White', N'#FFFFFF', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Jet Black',   N'#1A1A1A', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Matte Grey',  N'#808080', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Ocean Blue',  N'#0066CC', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
       (N'Coral Red',   N'#E63946', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

INSERT INTO dbo.VehiclePriceHistory (
    ModelId, ColorId, Price, PriceMonth, PriceYear, IsCurrentMonthPrice,
    ChangedBy, ChangedDate, CreatedDate, Notes, ModifiedDate, EffectiveFrom)
SELECT m.ModelId, c.ColorId,
    CASE m.ModelName
        WHEN N'Magnus EX'  THEN 99000.00
        WHEN N'Magnus Pro' THEN 105000.00
        WHEN N'Magnus Neo' THEN 85000.00
        WHEN N'Nexus EX'   THEN 115000.00
        WHEN N'Nexus ST'   THEN 120000.00
        WHEN N'Reo Li'     THEN 65000.00
        WHEN N'Reo Elite'  THEN 75000.00
        WHEN N'Zeal EX'    THEN 78000.00
        ELSE 90000.00
    END,
    8, 2026, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(),
    N'Ampere August 2026 pricing', SYSUTCDATETIME(), CAST('2026-08-01' AS DATE)
FROM dbo.VehicleModels m
CROSS JOIN dbo.VehicleColors c;

INSERT INTO dbo.CommissionRates (ModelId, CommissionAmount, StartMonth, StartYear, Notes, CreatedBy, CreatedDate)
SELECT m.ModelId,
    CASE m.ModelName
        WHEN N'Magnus EX'  THEN 2800.00
        WHEN N'Magnus Pro' THEN 3000.00
        WHEN N'Magnus Neo' THEN 2500.00
        WHEN N'Nexus EX'   THEN 3200.00
        WHEN N'Nexus ST'   THEN 3500.00
        WHEN N'Reo Li'     THEN 2000.00
        WHEN N'Reo Elite'  THEN 2200.00
        WHEN N'Zeal EX'    THEN 2300.00
        ELSE 2500.00
    END,
    8, 2026, N'Ampere ' + m.ModelName + N' commission', 1, SYSUTCDATETIME()
FROM dbo.VehicleModels m;

-- Ampere showroom dealership names
UPDATE dbo.Dealerships SET
    DealershipName = CASE DealershipCode
        WHEN N'KARUR'    THEN N'Ampere Showroom Karur'
        WHEN N'NAMAKKAL' THEN N'Ampere Showroom Namakkal'
        WHEN N'SALEM'    THEN N'Ampere Showroom Salem'
        WHEN N'ERODE'    THEN N'Ampere Showroom Erode'
        ELSE DealershipName END,
    Email = CASE DealershipCode
        WHEN N'KARUR'    THEN N'karur@ampere.krs.com'
        WHEN N'NAMAKKAL' THEN N'namakkal@ampere.krs.com'
        WHEN N'SALEM'    THEN N'salem@ampere.krs.com'
        WHEN N'ERODE'    THEN N'erode@ampere.krs.com'
        ELSE Email END,
    ModifiedDate = SYSUTCDATETIME();

-- Finance options — Ampere showroom only (deactivate any other-brand sample rows)
UPDATE dbo.FinanceNames SET IsActive = 0, ModifiedDate = SYSUTCDATETIME()
WHERE FinanceName NOT LIKE N'AMPERE%';

IF NOT EXISTS (SELECT 1 FROM dbo.FinanceNames WHERE FinanceName = N'AMPERE EASY FINANCE')
    INSERT INTO dbo.FinanceNames (FinanceName, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'AMPERE EASY FINANCE', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.FinanceNames WHERE FinanceName = N'AMPERE INSTANT LOAN')
    INSERT INTO dbo.FinanceNames (FinanceName, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'AMPERE INSTANT LOAN', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.FinanceNames WHERE FinanceName = N'AMPERE ZERO DOWN EMI')
    INSERT INTO dbo.FinanceNames (FinanceName, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'AMPERE ZERO DOWN EMI', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

COMMIT TRAN;

PRINT '=== Ampere sample data applied ===';
SELECT ModelName FROM dbo.VehicleModels ORDER BY ModelId;
GO

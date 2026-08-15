/*
  LOCAL DB ONLY — base reference data after truncate.
  Ampere e-vehicle showroom (KRS dealer network).
  Run before SEED_SUBDEALERS_AUG26_UPDATE.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

------------------------------------------------------------
-- 1) Admin login (required before HIERARCHY_SCHEMA admin mapping)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'admin', N'admin@krsdealers.com', N'Admin@123', N'Ampere', N'Admin', 1, N'9876543210', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

------------------------------------------------------------
-- 2) Ampere vehicle models & colors
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.VehicleModels)
BEGIN
    INSERT INTO dbo.VehicleModels (ModelName, Description, IsActive, CreatedBy, CreatedDate, ModifiedDate)
    VALUES (N'Magnus EX',   N'Ampere Magnus EX electric scooter',   1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Magnus Pro',  N'Ampere Magnus Pro electric scooter',  1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Magnus Neo',  N'Ampere Magnus Neo electric scooter',  1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Nexus EX',    N'Ampere Nexus EX electric scooter',    1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Nexus ST',    N'Ampere Nexus ST electric scooter',    1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Reo Li',      N'Ampere Reo Li electric scooter',      1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Reo Elite',   N'Ampere Reo Elite electric scooter',   1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Zeal EX',     N'Ampere Zeal EX electric scooter',     1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.VehicleColors)
BEGIN
    INSERT INTO dbo.VehicleColors (ColorName, HexCode, IsActive, CreatedBy, CreatedDate, ModifiedDate)
    VALUES (N'Pearl White',  N'#FFFFFF', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Jet Black',    N'#1A1A1A', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Matte Grey',   N'#808080', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Ocean Blue',   N'#0066CC', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'Coral Red',    N'#E63946', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.VehiclePriceHistory)
BEGIN
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
        N'Ampere August 2026 pricing — ' + m.ModelName + N' / ' + c.ColorName,
        SYSUTCDATETIME(), CAST('2026-08-01' AS DATE)
    FROM dbo.VehicleModels m
    CROSS JOIN dbo.VehicleColors c
    WHERE m.ModelName IN (N'Magnus EX', N'Magnus Pro', N'Magnus Neo', N'Nexus EX', N'Nexus ST', N'Reo Li', N'Reo Elite', N'Zeal EX');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CommissionRates)
BEGIN
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
    FROM dbo.VehicleModels m
    WHERE m.ModelName IN (N'Magnus EX', N'Magnus Pro', N'Magnus Neo', N'Nexus EX', N'Nexus ST', N'Reo Li', N'Reo Elite', N'Zeal EX');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.FinanceNames)
BEGIN
    INSERT INTO dbo.FinanceNames (FinanceName, IsActive, CreatedDate, ModifiedDate)
    VALUES (N'AMPERE EASY FINANCE', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'AMPERE INSTANT LOAN', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (N'AMPERE ZERO DOWN EMI', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

PRINT '=== Ampere base reference data ready ===';
SELECT COUNT(*) AS VehicleModels FROM dbo.VehicleModels;
SELECT COUNT(*) AS VehicleColors FROM dbo.VehicleColors;
SELECT COUNT(*) AS AdminUsers FROM dbo.Users WHERE Username = N'admin';
GO

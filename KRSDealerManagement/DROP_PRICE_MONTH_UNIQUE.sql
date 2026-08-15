-- Remove old one-price-per-month constraint; allow multiple prices per month via EffectiveFrom
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_ModelColorMonthYear' AND parent_object_id = OBJECT_ID(N'dbo.VehiclePriceHistory'))
BEGIN
    ALTER TABLE dbo.VehiclePriceHistory DROP CONSTRAINT UQ_ModelColorMonthYear;
END
GO

-- Prevent duplicate effective date for same model + color
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_ModelColorEffectiveFrom' AND parent_object_id = OBJECT_ID(N'dbo.VehiclePriceHistory'))
BEGIN
    ALTER TABLE dbo.VehiclePriceHistory
        ADD CONSTRAINT UQ_ModelColorEffectiveFrom UNIQUE (ModelId, ColorId, EffectiveFrom);
END
GO

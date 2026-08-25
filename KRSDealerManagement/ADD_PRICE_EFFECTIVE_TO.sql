-- Add EffectiveTo to catalogue pricing (inclusive end date, like commission rates)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VehiclePriceHistory') AND name = 'EffectiveTo')
    ALTER TABLE dbo.VehiclePriceHistory ADD EffectiveTo DATE NULL;
GO

UPDATE dbo.VehiclePriceHistory
SET EffectiveTo = EOMONTH(ISNULL(EffectiveFrom, DATEFROMPARTS(PriceYear, PriceMonth, 1)))
WHERE EffectiveTo IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VehiclePriceHistory') AND name = 'EffectiveTo')
    ALTER TABLE dbo.VehiclePriceHistory ALTER COLUMN EffectiveTo DATE NOT NULL;
GO

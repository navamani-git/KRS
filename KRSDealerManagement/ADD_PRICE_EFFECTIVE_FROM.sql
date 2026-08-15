-- Allow multiple price entries per model/color/month with effective dates
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VehiclePriceHistory') AND name = 'EffectiveFrom')
    ALTER TABLE dbo.VehiclePriceHistory ADD EffectiveFrom DATE NULL;
GO

UPDATE dbo.VehiclePriceHistory
SET EffectiveFrom = DATEFROMPARTS(PriceYear, PriceMonth, 1)
WHERE EffectiveFrom IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.VehiclePriceHistory') AND name = 'EffectiveFrom')
    ALTER TABLE dbo.VehiclePriceHistory ALTER COLUMN EffectiveFrom DATE NOT NULL;
GO

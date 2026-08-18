-- Commission rates: date-level effective from (supports multiple rates within same month)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CommissionRates') AND name = N'EffectiveFrom')
BEGIN
    ALTER TABLE dbo.CommissionRates ADD EffectiveFrom DATE NULL;
    PRINT 'Added EffectiveFrom.';
END
GO

-- Backfill from existing StartYear/StartMonth
UPDATE dbo.CommissionRates
SET EffectiveFrom = DATEFROMPARTS(StartYear, StartMonth, 1)
WHERE EffectiveFrom IS NULL;
GO

-- Default any remaining rows
UPDATE dbo.CommissionRates
SET EffectiveFrom = CAST(GETUTCDATE() AS DATE)
WHERE EffectiveFrom IS NULL;
GO

ALTER TABLE dbo.CommissionRates ALTER COLUMN EffectiveFrom DATE NOT NULL;
GO

PRINT 'Commission EffectiveFrom ready.';

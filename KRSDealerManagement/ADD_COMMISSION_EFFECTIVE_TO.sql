-- Commission rates: required effective-to date for bounded rate periods
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CommissionRates') AND name = N'EffectiveTo')
BEGIN
    ALTER TABLE dbo.CommissionRates ADD EffectiveTo DATE NULL;
    PRINT 'Added EffectiveTo.';
END
GO

-- Backfill: use expiry month end, else last day of effective-from month
UPDATE dbo.CommissionRates
SET EffectiveTo = CASE
    WHEN ExpiryYear IS NOT NULL AND ExpiryMonth IS NOT NULL
        THEN EOMONTH(DATEFROMPARTS(ExpiryYear, ExpiryMonth, 1))
    WHEN EffectiveFrom IS NOT NULL
        THEN EOMONTH(EffectiveFrom)
    ELSE EOMONTH(GETUTCDATE())
END
WHERE EffectiveTo IS NULL;
GO

UPDATE dbo.CommissionRates
SET EffectiveTo = EOMONTH(GETUTCDATE())
WHERE EffectiveTo IS NULL;
GO

ALTER TABLE dbo.CommissionRates ALTER COLUMN EffectiveTo DATE NOT NULL;
GO

PRINT 'Commission EffectiveTo ready.';

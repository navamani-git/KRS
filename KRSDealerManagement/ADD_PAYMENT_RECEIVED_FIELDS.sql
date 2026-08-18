-- Payment approval: actual received amount/date (run on local + production)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'ActualReceivedAmount')
BEGIN
    ALTER TABLE dbo.Payments ADD ActualReceivedAmount DECIMAL(18, 2) NULL;
    PRINT 'Added ActualReceivedAmount.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'ActualReceivedDate')
BEGIN
    ALTER TABLE dbo.Payments ADD ActualReceivedDate DATE NULL;
    PRINT 'Added ActualReceivedDate.';
END
GO

-- Backfill approved payments
UPDATE dbo.Payments
SET ActualReceivedAmount = Amount,
    ActualReceivedDate = CAST(PaymentDate AS DATE)
WHERE Status = 1
  AND ActualReceivedAmount IS NULL;
GO

PRINT 'Payment received amount/date columns ready.';

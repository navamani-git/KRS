-- Commission rejection / payment timestamps on CommissionHistory
IF COL_LENGTH('dbo.CommissionHistory', 'RejectedBy') IS NULL
    ALTER TABLE dbo.CommissionHistory ADD RejectedBy INT NULL;
GO
IF COL_LENGTH('dbo.CommissionHistory', 'RejectedDate') IS NULL
    ALTER TABLE dbo.CommissionHistory ADD RejectedDate DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.CommissionHistory', 'PaidDate') IS NULL
    ALTER TABLE dbo.CommissionHistory ADD PaidDate DATETIME2 NULL;
GO

-- Backfill rejection date from ModifiedDate for already-rejected rows
UPDATE dbo.CommissionHistory
SET RejectedDate = ModifiedDate
WHERE CommissionStatus = 3
  AND RejectedDate IS NULL;
GO

-- Backfill paid date from approved date for paid rows
UPDATE dbo.CommissionHistory
SET PaidDate = ApprovedDate
WHERE CommissionStatus = 2
  AND PaidDate IS NULL
  AND ApprovedDate IS NOT NULL;
GO

PRINT 'CommissionHistory rejection/payment date columns ready.';
GO

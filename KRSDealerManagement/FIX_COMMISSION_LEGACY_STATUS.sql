-- Normalize legacy CommissionHistory status values to current app schema.
-- Legacy: 1=Pending, 2=Approved, 3=Rejected
-- Current: 0=Pending, 1=Approved, 2=Paid, 3=Rejected

UPDATE dbo.CommissionHistory
SET CommissionStatus = 0
WHERE CommissionStatus = 1
  AND ApprovedDate IS NULL
  AND ApprovedBy IS NULL;
GO

UPDATE dbo.CommissionHistory
SET CommissionStatus = 2
WHERE CommissionStatus = 2
  AND ApprovedDate IS NOT NULL;
GO

UPDATE dbo.StatusLookups
SET StatusName = N'Awaiting Approval',
    StatusCode = N'AWAITING_APPROVAL'
WHERE Category = N'COMMISSION'
  AND StatusValue = 0;
GO

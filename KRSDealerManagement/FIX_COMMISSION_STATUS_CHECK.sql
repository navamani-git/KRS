-- Fix CK_CommissionStatus: allow Pending=0 (matches StatusLookups COMMISSION + CommissionStatusEnum)
-- Status values: 0=Pending, 1=Approved, 2=Paid, 3=Rejected

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_CommissionStatus'
      AND parent_object_id = OBJECT_ID(N'dbo.CommissionHistory'))
BEGIN
    ALTER TABLE dbo.CommissionHistory DROP CONSTRAINT CK_CommissionStatus;
END
GO

ALTER TABLE dbo.CommissionHistory
    ADD CONSTRAINT CK_CommissionStatus
    CHECK (CommissionStatus IN (0, 1, 2, 3));
GO

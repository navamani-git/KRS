-- =============================================================================
-- Apply on BOTH databases (see Requirement/dbchanges):
--   1) krsenterprise.in  (production)
--   2) localhost\SQLEXPRESS (local dev)
-- =============================================================================

-- Price: effective-from date (multiple prices per month)
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

-- Price: drop old one-per-month unique key
IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_ModelColorMonthYear' AND parent_object_id = OBJECT_ID(N'dbo.VehiclePriceHistory'))
    ALTER TABLE dbo.VehiclePriceHistory DROP CONSTRAINT UQ_ModelColorMonthYear;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_ModelColorEffectiveFrom' AND parent_object_id = OBJECT_ID(N'dbo.VehiclePriceHistory'))
    ALTER TABLE dbo.VehiclePriceHistory
        ADD CONSTRAINT UQ_ModelColorEffectiveFrom UNIQUE (ModelId, ColorId, EffectiveFrom);
GO

-- Vehicles: notes for price revision audit trail
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Vehicles') AND name = 'Notes')
    ALTER TABLE dbo.Vehicles ADD Notes NVARCHAR(MAX) NULL;
GO

-- Commission: allow Pending=0 in CommissionStatus (was 1,2,3 only)
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_CommissionStatus'
      AND parent_object_id = OBJECT_ID(N'dbo.CommissionHistory'))
    ALTER TABLE dbo.CommissionHistory DROP CONSTRAINT CK_CommissionStatus;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_CommissionStatus'
      AND parent_object_id = OBJECT_ID(N'dbo.CommissionHistory'))
    ALTER TABLE dbo.CommissionHistory
        ADD CONSTRAINT CK_CommissionStatus
        CHECK (CommissionStatus IN (0, 1, 2, 3));
GO

-- AccountTransactions: allow CommissionApproved=7, CommissionRejected=8, ManualAdjustment=9
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_TransactionType'
      AND parent_object_id = OBJECT_ID(N'dbo.AccountTransactions'))
    ALTER TABLE dbo.AccountTransactions DROP CONSTRAINT CK_TransactionType;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_TransactionType'
      AND parent_object_id = OBJECT_ID(N'dbo.AccountTransactions'))
    ALTER TABLE dbo.AccountTransactions
        ADD CONSTRAINT CK_TransactionType
        CHECK (TransactionType IN (1, 2, 3, 4, 5, 6, 7, 8, 9));
GO

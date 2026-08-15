-- Fix CK_TransactionType: allow full app enum (1-9)
-- Commission approval uses 7 (CommissionApproved); constraint previously allowed only 1-6.

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_TransactionType'
      AND parent_object_id = OBJECT_ID(N'dbo.AccountTransactions'))
BEGIN
    ALTER TABLE dbo.AccountTransactions DROP CONSTRAINT CK_TransactionType;
END
GO

ALTER TABLE dbo.AccountTransactions
    ADD CONSTRAINT CK_TransactionType
    CHECK (TransactionType IN (1, 2, 3, 4, 5, 6, 7, 8, 9));
GO

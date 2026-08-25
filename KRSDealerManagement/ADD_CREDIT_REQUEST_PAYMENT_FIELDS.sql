-- Credit Request payment type + optional vehicle fields on Payments
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Payments') AND name = 'CreditRequestModelName')
BEGIN
    ALTER TABLE Payments ADD CreditRequestModelName NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Payments') AND name = 'CreditRequestColorName')
BEGIN
    ALTER TABLE Payments ADD CreditRequestColorName NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM PaymentTypes WHERE TypeCode = 'CREDIT_REQUEST')
BEGIN
    INSERT INTO PaymentTypes (TypeCode, TypeName, RequiresFinanceDetails, IsActive, SortOrder, CreatedDate)
    VALUES ('CREDIT_REQUEST', 'Credit Request', 0, 1, 99, GETUTCDATE());
END
GO

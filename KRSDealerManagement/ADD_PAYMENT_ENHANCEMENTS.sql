-- Payment enhancements: types from DB, finance names, proof uploads
IF OBJECT_ID('PaymentTypes') IS NULL
BEGIN
  CREATE TABLE PaymentTypes (
    PaymentTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeCode NVARCHAR(30) NOT NULL UNIQUE,
    TypeName NVARCHAR(100) NOT NULL,
    RequiresFinanceDetails BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
  );
END
GO
IF NOT EXISTS (SELECT 1 FROM PaymentTypes)
INSERT INTO PaymentTypes (TypeCode, TypeName, RequiresFinanceDetails, SortOrder) VALUES
 (N'CASH', N'Cash', 0, 1),
 (N'GPAY', N'Google Pay (GPay)', 0, 2),
 (N'NEFT', N'NEFT', 0, 3),
 (N'FINANCE', N'Finance', 1, 4),
 (N'OTHERS', N'Others', 0, 5);
GO
IF OBJECT_ID('FinanceNames') IS NULL
BEGIN
  CREATE TABLE FinanceNames (
    FinanceNameId INT IDENTITY(1,1) PRIMARY KEY,
    FinanceName NVARCHAR(150) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
  );
END
GO
IF COL_LENGTH('Payments','CustomerName') IS NULL ALTER TABLE Payments ADD CustomerName NVARCHAR(150) NULL;
IF COL_LENGTH('Payments','FinanceNameId') IS NULL ALTER TABLE Payments ADD FinanceNameId INT NULL;
IF COL_LENGTH('Payments','VinNumber') IS NULL ALTER TABLE Payments ADD VinNumber NVARCHAR(50) NULL;
IF COL_LENGTH('Payments','PaymentProofPath') IS NULL ALTER TABLE Payments ADD PaymentProofPath NVARCHAR(500) NULL;
IF COL_LENGTH('Payments','PaymentProof2Path') IS NULL ALTER TABLE Payments ADD PaymentProof2Path NVARCHAR(500) NULL;
IF COL_LENGTH('Payments','PaymentTypeId') IS NULL ALTER TABLE Payments ADD PaymentTypeId INT NULL;
GO

-- Multi-dealer hierarchy + staff roles
-- Roles: 1=SystemAdmin, 2=Subdealer, 3=FinanceAdmin, 4=DealerBranchManager

IF OBJECT_ID('Dealers') IS NULL
BEGIN
  CREATE TABLE Dealers (
    DealerId INT IDENTITY(1,1) PRIMARY KEY,
    DealerName NVARCHAR(150) NOT NULL,
    Location NVARCHAR(150) NULL,
    ContactPhone NVARCHAR(20) NULL,
    Email NVARCHAR(150) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  );
END
GO

IF COL_LENGTH('Users','DealerId') IS NULL
  ALTER TABLE Users ADD DealerId INT NULL;
GO

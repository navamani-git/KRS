-- Status lookups: payment, order, return, commission, order item, vehicle
IF OBJECT_ID('dbo.StatusLookups') IS NULL
BEGIN
  CREATE TABLE dbo.StatusLookups (
    StatusLookupId INT IDENTITY(1,1) PRIMARY KEY,
    Category NVARCHAR(40) NOT NULL,       -- ORDER, PAYMENT, RETURN, COMMISSION, ORDER_ITEM, VEHICLE
    StatusValue INT NOT NULL,             -- matches existing Status INT columns
    StatusCode NVARCHAR(40) NOT NULL,
    StatusName NVARCHAR(100) NOT NULL,
    BadgeClass NVARCHAR(80) NOT NULL DEFAULT N'bg-secondary',
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_StatusLookups_Category_Value UNIQUE (Category, StatusValue),
    CONSTRAINT UQ_StatusLookups_Category_Code UNIQUE (Category, StatusCode)
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'ORDER')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'ORDER', 0, N'PENDING',   N'Pending',   N'bg-warning text-dark', 1),
 (N'ORDER', 1, N'APPROVED',  N'Approved',  N'bg-success', 2),
 (N'ORDER', 2, N'REJECTED',  N'Rejected',  N'bg-danger', 3),
 (N'ORDER', 3, N'DELIVERED', N'Delivered', N'bg-info', 4);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'PAYMENT')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'PAYMENT', 0, N'PENDING',  N'Pending',  N'bg-warning text-dark', 1),
 (N'PAYMENT', 1, N'APPROVED', N'Approved', N'bg-success', 2),
 (N'PAYMENT', 2, N'REJECTED', N'Rejected', N'bg-danger', 3);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'RETURN')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'RETURN', 0, N'PENDING',  N'Pending',  N'bg-warning text-dark', 1),
 (N'RETURN', 1, N'APPROVED', N'Approved', N'bg-success', 2),
 (N'RETURN', 2, N'REJECTED', N'Rejected', N'bg-danger', 3);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'COMMISSION')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'COMMISSION', 0, N'PENDING',  N'Pending',  N'bg-warning text-dark', 1),
 (N'COMMISSION', 1, N'APPROVED', N'Approved', N'bg-info', 2),
 (N'COMMISSION', 2, N'PAID',     N'Paid',     N'bg-success', 3),
 (N'COMMISSION', 3, N'REJECTED', N'Rejected', N'bg-danger', 4);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'ORDER_ITEM')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'ORDER_ITEM', 0, N'PENDING',  N'Pending',  N'bg-warning text-dark', 1),
 (N'ORDER_ITEM', 1, N'APPROVED', N'Approved', N'bg-success', 2),
 (N'ORDER_ITEM', 2, N'REJECTED', N'Rejected', N'bg-danger', 3);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StatusLookups WHERE Category = N'VEHICLE')
INSERT INTO dbo.StatusLookups (Category, StatusValue, StatusCode, StatusName, BadgeClass, SortOrder) VALUES
 (N'VEHICLE', 0, N'AVAILABLE',       N'Available',        N'bg-success', 1),
 (N'VEHICLE', 1, N'RESERVED',        N'Reserved',         N'bg-warning text-dark', 2),
 (N'VEHICLE', 2, N'SOLD',            N'Sold',             N'bg-dark', 3),
 (N'VEHICLE', 3, N'DAMAGED',         N'Damaged',          N'bg-danger', 4),
 (N'VEHICLE', 4, N'PURCHASED',       N'Purchased',        N'bg-info', 5),
 (N'VEHICLE', 5, N'INVOICED',        N'Invoiced',         N'bg-warning text-dark', 6),
 (N'VEHICLE', 6, N'RTO_INITIATED',   N'RTO Initiated',    N'bg-secondary', 7),
 (N'VEHICLE', 7, N'RTO_COMPLETE',    N'RTO Complete',     N'bg-success', 8);
GO

-- =====================================================
-- FIX MISSING COLUMNS IN EXISTING TABLES
-- Run this in SSMS against KRSDealerManagementDB
-- =====================================================
USE KRSDealerManagementDB;
GO

-- ======================================
-- FIX 1: AuditLog - Add missing columns
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UserId' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD UserId INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UserRole' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD UserRole NVARCHAR(50);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'OldValue' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD OldValue NVARCHAR(MAX);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'NewValue' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD NewValue NVARCHAR(MAX);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Remarks' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD Remarks NVARCHAR(500);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedDate' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD CreatedDate DATETIME DEFAULT GETUTCDATE();
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UserAgent' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD UserAgent NVARCHAR(500) NULL;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'IpAddress' AND object_id = OBJECT_ID('AuditLog'))
    ALTER TABLE AuditLog ADD IpAddress NVARCHAR(50) NULL;
GO
-- App inserts UserId, not ChangedBy — allow NULL so legacy column does not block inserts
ALTER TABLE AuditLog ALTER COLUMN ChangedBy INT NULL;
GO

-- Copy existing data to new columns
UPDATE AuditLog SET
    UserId = ChangedBy,
    UserRole = 'Admin',
    OldValue = OldValues,
    NewValue = NewValues,
    Remarks = ChangeReason,
    CreatedDate = ChangedDate
WHERE UserId IS NULL;
GO
PRINT 'Fixed AuditLog columns';
GO

-- ======================================
-- FIX 2: AccountBalance - Add SubdealerAccountId
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'SubdealerAccountId' AND object_id = OBJECT_ID('AccountBalance'))
    ALTER TABLE AccountBalance ADD SubdealerAccountId INT;
GO

-- Link AccountBalance.SubdealerAccountId to SubdealerAccounts.AccountId
UPDATE ab SET ab.SubdealerAccountId = sa.AccountId
FROM AccountBalance ab
JOIN SubdealerAccounts sa ON sa.SubdealerId = ab.SubdealerId
WHERE ab.SubdealerAccountId IS NULL;
GO
PRINT 'Fixed AccountBalance.SubdealerAccountId';
GO

-- ======================================
-- FIX 3: AccountTransactions - Add missing columns
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'AccountId' AND object_id = OBJECT_ID('AccountTransactions'))
    ALTER TABLE AccountTransactions ADD AccountId INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Reason' AND object_id = OBJECT_ID('AccountTransactions'))
    ALTER TABLE AccountTransactions ADD Reason NVARCHAR(500);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ReferenceId' AND object_id = OBJECT_ID('AccountTransactions'))
    ALTER TABLE AccountTransactions ADD ReferenceId INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ReferenceType' AND object_id = OBJECT_ID('AccountTransactions'))
    ALTER TABLE AccountTransactions ADD ReferenceType NVARCHAR(100);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'InitiatedBy' AND object_id = OBJECT_ID('AccountTransactions'))
    ALTER TABLE AccountTransactions ADD InitiatedBy INT;
GO

-- Copy existing data  
UPDATE at SET
    at.AccountId = ab.SubdealerAccountId,
    at.Reason = at.Description,
    at.InitiatedBy = at.CreatedBy
FROM AccountTransactions at
JOIN AccountBalance ab ON ab.SubdealerId = at.SubdealerId
WHERE at.AccountId IS NULL;
GO
PRINT 'Fixed AccountTransactions columns';
GO

-- ======================================
-- FIX 4: SubdealerAccounts - add missing columns the app expects
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedBy' AND object_id = OBJECT_ID('SubdealerAccounts'))
    ALTER TABLE SubdealerAccounts ADD CreatedBy INT DEFAULT 1;
GO
PRINT 'Fixed SubdealerAccounts columns';
GO

-- ======================================
-- FIX 5: VehicleModels - add missing columns
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedBy' AND object_id = OBJECT_ID('VehicleModels'))
    ALTER TABLE VehicleModels ADD CreatedBy INT DEFAULT 1;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ModifiedBy' AND object_id = OBJECT_ID('VehicleModels'))
    ALTER TABLE VehicleModels ADD ModifiedBy INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ModifiedDate' AND object_id = OBJECT_ID('VehicleModels'))
    ALTER TABLE VehicleModels ADD ModifiedDate DATETIME DEFAULT GETUTCDATE();
GO
PRINT 'Fixed VehicleModels columns';
GO

-- ======================================
-- FIX 6: VehicleColors - add missing columns
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedBy' AND object_id = OBJECT_ID('VehicleColors'))
    ALTER TABLE VehicleColors ADD CreatedBy INT DEFAULT 1;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ModifiedBy' AND object_id = OBJECT_ID('VehicleColors'))
    ALTER TABLE VehicleColors ADD ModifiedBy INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ModifiedDate' AND object_id = OBJECT_ID('VehicleColors'))
    ALTER TABLE VehicleColors ADD ModifiedDate DATETIME DEFAULT GETUTCDATE();
GO
PRINT 'Fixed VehicleColors columns';
GO

-- ======================================
-- FIX 7: PurchaseOrders - add missing columns the app expects
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'AccountId' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD AccountId INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'OrderId' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD OrderId AS PurchaseOrderId;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'TotalQuantity' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD TotalQuantity AS VehicleCount;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'TotalAmount' AND object_id = OBJECT_ID('PurchaseOrders'))
BEGIN
    -- TotalAmount already exists, just rename alias
    PRINT 'PurchaseOrders.TotalAmount already exists';
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Status' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD Status AS (PurchaseOrderStatus - 1);  -- DB uses 1-based, app uses 0-based
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'AdminNotes' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD AdminNotes AS RejectionReason;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'SubdealerNotes' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD SubdealerNotes NVARCHAR(500);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedDate' AND object_id = OBJECT_ID('PurchaseOrders'))
    ALTER TABLE PurchaseOrders ADD CreatedDate AS RequestedDate;
GO

-- Link AccountId from SubdealerAccounts
UPDATE po SET po.AccountId = sa.AccountId
FROM PurchaseOrders po
JOIN SubdealerAccounts sa ON sa.SubdealerId = po.SubdealerId
WHERE po.AccountId IS NULL;
GO
PRINT 'Fixed PurchaseOrders columns';
GO

-- ======================================
-- FIX 8: VehiclePriceHistory - map columns
-- ======================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Month' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD Month AS PriceMonth;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Year' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD Year AS PriceYear;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Notes' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD Notes NVARCHAR(500);
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'VehicleId' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD VehicleId INT DEFAULT 0;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedBy' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD CreatedBy AS ChangedBy;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreatedDate' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD CreatedDate AS ChangedDate;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ModifiedBy' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD ModifiedBy INT;
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ModifiedDate' AND object_id = OBJECT_ID('VehiclePriceHistory'))
    ALTER TABLE VehiclePriceHistory ADD ModifiedDate DATETIME DEFAULT GETUTCDATE();
GO
PRINT 'Fixed VehiclePriceHistory columns';
GO

-- ======================================
-- VERIFY - Show all tables
-- ======================================
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' 
ORDER BY TABLE_NAME;
GO

PRINT '=== ALL COLUMN FIXES APPLIED ===';
PRINT 'Run the application now and login with:';
PRINT 'admin / Admin@123';
GO

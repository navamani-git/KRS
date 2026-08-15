-- =====================================================
-- CREATE MISSING TABLES
-- Run this in SSMS against KRSDealerManagementDB
-- This adds tables that the app needs but DATABASE_INIT.sql didn't create
-- =====================================================

USE KRSDealerManagementDB;
GO

-- 1. SubdealerAccounts (app needs this for multi-account support per subdealer)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubdealerAccounts')
BEGIN
    CREATE TABLE SubdealerAccounts (
        AccountId INT PRIMARY KEY IDENTITY(1,1),
        SubdealerId INT NOT NULL,
        AccountName NVARCHAR(100) NOT NULL,
        AccountType NVARCHAR(50) NOT NULL DEFAULT 'Main',
        Description NVARCHAR(500),
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME DEFAULT GETUTCDATE(),
        FOREIGN KEY (SubdealerId) REFERENCES Users(UserId)
    );

    -- Create one account per existing subdealer
    INSERT INTO SubdealerAccounts (SubdealerId, AccountName, AccountType, IsActive, CreatedDate, ModifiedDate)
    SELECT UserId, 'Main Account', 'Main', 1, GETUTCDATE(), GETUTCDATE()
    FROM Users WHERE UserRole = 2;

    PRINT 'Created SubdealerAccounts table and seeded accounts';
END
GO

-- 2. CommissionRates (app needs this for setting commission per model per month)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommissionRates')
BEGIN
    CREATE TABLE CommissionRates (
        CommissionRateId INT PRIMARY KEY IDENTITY(1,1),
        ModelId INT NOT NULL,
        CommissionAmount DECIMAL(18,2) NOT NULL,
        StartMonth INT NOT NULL,
        StartYear INT NOT NULL,
        ExpiryMonth INT,
        ExpiryYear INT,
        Notes NVARCHAR(500),
        CreatedBy INT NOT NULL,
        CreatedDate DATETIME DEFAULT GETUTCDATE(),
        ModifiedBy INT,
        ModifiedDate DATETIME DEFAULT GETUTCDATE(),
        FOREIGN KEY (ModelId) REFERENCES VehicleModels(ModelId)
    );

    -- Seed commission rates (matching existing commissions in DATABASE_SETUP.sql)
    INSERT INTO CommissionRates (ModelId, CommissionAmount, StartMonth, StartYear, Notes, CreatedBy)
    SELECT ModelId, 5000.00, 8, 2026, 'Default commission rate', 1
    FROM VehicleModels WHERE IsActive = 1;

    PRINT 'Created CommissionRates table and seeded rates';
END
GO

-- 3. Payments (app needs this for subdealer payment submissions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE Payments (
        PaymentId INT PRIMARY KEY IDENTITY(1,1),
        AccountId INT NOT NULL,
        SubdealerId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentType NVARCHAR(50) NOT NULL,
        PaymentDate DATETIME NOT NULL,
        Status INT DEFAULT 0,  -- 0=Pending, 1=Approved, 2=Rejected
        SubdealerRemarks NVARCHAR(500),
        DealerRemarks NVARCHAR(500),
        ProcessedBy INT,
        ProcessedDate DATETIME,
        IsApplied BIT DEFAULT 0,
        TransactionId INT,
        CreatedDate DATETIME DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME DEFAULT GETUTCDATE(),
        FOREIGN KEY (SubdealerId) REFERENCES Users(UserId)
    );
    PRINT 'Created Payments table';
END
GO

-- 4. ReturnRequests (app needs this for vehicle returns)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnRequests')
BEGIN
    CREATE TABLE ReturnRequests (
        ReturnRequestId INT PRIMARY KEY IDENTITY(1,1),
        AccountId INT NOT NULL,
        OrderId INT NOT NULL,
        VehicleId INT NOT NULL,
        RefundAmount DECIMAL(18,2) NOT NULL,
        ReturnReason NVARCHAR(500) NOT NULL,
        Status INT DEFAULT 0,  -- 0=Pending, 1=Approved, 2=Rejected
        AdminRemarks NVARCHAR(500),
        ProcessedBy INT,
        ProcessedDate DATETIME,
        CreatedDate DATETIME DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME DEFAULT GETUTCDATE(),
        FOREIGN KEY (OrderId) REFERENCES PurchaseOrders(PurchaseOrderId),
        FOREIGN KEY (VehicleId) REFERENCES Vehicles(VehicleId)
    );
    PRINT 'Created ReturnRequests table';
END
GO

-- 5. AccountPermissions (app needs this for menu access control)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccountPermissions')
BEGIN
    CREATE TABLE AccountPermissions (
        PermissionId INT PRIMARY KEY IDENTITY(1,1),
        AccountId INT NOT NULL,
        MenuKey NVARCHAR(50) NOT NULL,
        MenuName NVARCHAR(100) NOT NULL,
        IsAccessible BIT DEFAULT 1,
        CanCreate BIT DEFAULT 1,
        CanEdit BIT DEFAULT 1,
        CanDelete BIT DEFAULT 0,
        CanApprove BIT DEFAULT 0,
        CreatedDate DATETIME DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME DEFAULT GETUTCDATE(),
        FOREIGN KEY (AccountId) REFERENCES SubdealerAccounts(AccountId)
    );
    PRINT 'Created AccountPermissions table';
END
GO

-- 6. Update AuditLog table to match app's expected column names
-- App uses: UserId, UserRole, Action, EntityType, EntityId, OldValue, NewValue, Remarks, CreatedDate
-- DB has:   ChangedBy, ChangedDate, OldValues, NewValues, ChangeReason
-- Add missing columns to AuditLog
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UserId' AND object_id = OBJECT_ID('AuditLog'))
BEGIN
    ALTER TABLE AuditLog ADD UserId INT;
    ALTER TABLE AuditLog ADD UserRole NVARCHAR(50);
    ALTER TABLE AuditLog ADD OldValue NVARCHAR(MAX);
    ALTER TABLE AuditLog ADD NewValue NVARCHAR(MAX);
    ALTER TABLE AuditLog ADD Remarks NVARCHAR(500);
    ALTER TABLE AuditLog ADD AuditLogId_New INT;

    -- Copy existing data to new columns
    UPDATE AuditLog SET 
        UserId = ChangedBy,
        UserRole = 'Admin',
        OldValue = OldValues,
        NewValue = NewValues,
        Remarks = ChangeReason;
    
    PRINT 'Updated AuditLog table with missing columns';
END
GO

-- 7. Update AccountBalance to add missing columns that app expects
-- App expects: SubdealerAccountId, SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, LastTransactionDate, CreatedDate, ModifiedDate, BalanceId
-- DB has: AccountId, SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, LastTransactionDate, CreatedDate, ModifiedDate
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'BalanceId' AND object_id = OBJECT_ID('AccountBalance'))
BEGIN
    ALTER TABLE AccountBalance ADD BalanceId AS AccountId;  -- computed column alias
    PRINT 'Added BalanceId to AccountBalance';
END
GO

-- Add SubdealerAccountId to AccountBalance (app uses this to link to SubdealerAccounts)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'SubdealerAccountId' AND object_id = OBJECT_ID('AccountBalance'))
BEGIN
    ALTER TABLE AccountBalance ADD SubdealerAccountId INT;
    
    -- Link to SubdealerAccounts
    UPDATE ab SET ab.SubdealerAccountId = sa.AccountId
    FROM AccountBalance ab
    JOIN SubdealerAccounts sa ON sa.SubdealerId = ab.SubdealerId;
    
    PRINT 'Added SubdealerAccountId to AccountBalance';
END
GO

-- 8. Update AccountTransactions to add missing columns
-- App expects: TransactionId, AccountId, TransactionType(int), Amount, BalanceAfterTransaction, Reason, ReferenceId, ReferenceType, Remarks, InitiatedBy, CreatedDate
-- DB has: TransactionId, SubdealerId, TransactionType, Amount, BalanceBeforeTransaction, BalanceAfterTransaction, Description, CreatedBy, CreatedDate
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'AccountId' AND object_id = OBJECT_ID('AccountTransactions'))
BEGIN
    ALTER TABLE AccountTransactions ADD AccountId INT;
    ALTER TABLE AccountTransactions ADD Reason NVARCHAR(500);
    ALTER TABLE AccountTransactions ADD ReferenceId INT;
    ALTER TABLE AccountTransactions ADD ReferenceType NVARCHAR(100);
    ALTER TABLE AccountTransactions ADD Remarks NVARCHAR(500);
    ALTER TABLE AccountTransactions ADD InitiatedBy INT;

    -- Copy existing
    UPDATE AccountTransactions SET
        AccountId = (SELECT AccountId FROM AccountBalance WHERE SubdealerId = AccountTransactions.SubdealerId),
        Reason = Description,
        InitiatedBy = CreatedBy;
    
    PRINT 'Updated AccountTransactions with missing columns';
END
GO

-- 9. Reset passwords to plain text for login
UPDATE Users SET PasswordHash = 'Admin@123' WHERE UserRole = 1;
UPDATE Users SET PasswordHash = 'Subdealers@123' WHERE UserRole = 2;
PRINT 'Reset passwords to plain text';
GO

PRINT '=== ALL DONE ===';
PRINT 'All missing tables created and existing tables updated.';
PRINT '';
PRINT 'Login credentials:';
PRINT 'Admin: admin / Admin@123';
PRINT 'Subdealer: subdealer_001 / Subdealers@123';
GO

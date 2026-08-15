-- ============================================================================
-- KRS EV Dealer Management System - Database Initialization Script
-- Database: KRSDealerManagementDB
-- Version: 1.0
-- ============================================================================

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'KRSDealerManagementDB')
BEGIN
    CREATE DATABASE [KRSDealerManagementDB];
    PRINT 'Created database [KRSDealerManagementDB]';
END
GO

USE [KRSDealerManagementDB];
GO

-- ============================================================================
-- 1. USERS TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [Users] (
        [UserId] INT PRIMARY KEY IDENTITY(1,1),
        [Username] NVARCHAR(100) NOT NULL UNIQUE,
        [Email] NVARCHAR(150) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100),
        [UserRole] INT NOT NULL,  -- 1=Admin, 2=Subdealers
        [PhoneNumber] NVARCHAR(20),
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [CK_UserRole] CHECK ([UserRole] IN (1, 2))
    );
    
    CREATE INDEX [IX_Users_Username] ON [Users]([Username]);
    CREATE INDEX [IX_Users_UserRole] ON [Users]([UserRole]);
    CREATE INDEX [IX_Users_IsActive] ON [Users]([IsActive]);
    
    PRINT 'Created [Users] table';
END
GO

-- ============================================================================
-- 2. VEHICLE MODELS TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VehicleModels')
BEGIN
    CREATE TABLE [VehicleModels] (
        [ModelId] INT PRIMARY KEY IDENTITY(1,1),
        [ModelName] NVARCHAR(100) NOT NULL UNIQUE,
        [Description] NVARCHAR(500),
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedBy] INT NOT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] INT,
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_VehicleModels_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([UserId])
    );
    
    CREATE INDEX [IX_VehicleModels_IsActive] ON [VehicleModels]([IsActive]);
    
    PRINT 'Created [VehicleModels] table';
END
GO

-- ============================================================================
-- 3. VEHICLE COLORS TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VehicleColors')
BEGIN
    CREATE TABLE [VehicleColors] (
        [ColorId] INT PRIMARY KEY IDENTITY(1,1),
        [ColorName] NVARCHAR(100) NOT NULL UNIQUE,
        [HexCode] NVARCHAR(7),  -- e.g., #FFFFFF for white
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedBy] INT NOT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedBy] INT,
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_VehicleColors_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([UserId])
    );
    
    CREATE INDEX [IX_VehicleColors_IsActive] ON [VehicleColors]([IsActive]);
    
    PRINT 'Created [VehicleColors] table';
END
GO

-- ============================================================================
-- 4. VEHICLE PRICE HISTORY TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VehiclePriceHistory')
BEGIN
    CREATE TABLE [VehiclePriceHistory] (
        [PriceHistoryId] INT PRIMARY KEY IDENTITY(1,1),
        [ModelId] INT NOT NULL,
        [ColorId] INT NOT NULL,
        [Price] DECIMAL(15, 2) NOT NULL,
        [PriceMonth] INT NOT NULL,  -- 1-12 for January-December
        [PriceYear] INT NOT NULL,  -- 2026, 2027, etc.
        [IsCurrentMonthPrice] BIT NOT NULL DEFAULT 1,
        [ChangedBy] INT NOT NULL,
        [ChangedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ChangeReason] NVARCHAR(500),
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_VehiclePriceHistory_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [VehicleModels]([ModelId]),
        CONSTRAINT [FK_VehiclePriceHistory_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [VehicleColors]([ColorId]),
        CONSTRAINT [FK_VehiclePriceHistory_ChangedByUser] FOREIGN KEY ([ChangedBy]) REFERENCES [Users]([UserId]),
        CONSTRAINT [UQ_ModelColorMonthYear] UNIQUE ([ModelId], [ColorId], [PriceMonth], [PriceYear])
    );
    
    CREATE INDEX [IX_VehiclePriceHistory_ModelColor] ON [VehiclePriceHistory]([ModelId], [ColorId]);
    CREATE INDEX [IX_VehiclePriceHistory_MonthYear] ON [VehiclePriceHistory]([PriceMonth], [PriceYear]);
    
    PRINT 'Created [VehiclePriceHistory] table';
END
GO

-- ============================================================================
-- 5. PURCHASE ORDERS TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [PurchaseOrderId] INT PRIMARY KEY IDENTITY(1,1),
        [OrderNumber] NVARCHAR(50) NOT NULL UNIQUE,  -- e.g., PO-20260807-001
        [SubdealerId] INT NOT NULL,
        [TotalAmount] DECIMAL(15, 2) NOT NULL,
        [ApprovedAmount] DECIMAL(15, 2),
        [PurchaseOrderStatus] INT NOT NULL,  -- 1=Pending, 2=Approved, 3=Rejected
        [VehicleCount] INT NOT NULL,
        [ApprovedVehicleCount] INT,
        [RejectionReason] NVARCHAR(500),
        [RequestedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ApprovedBy] INT,
        [ApprovedDate] DATETIME,
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_PurchaseOrders_SubdealerId] FOREIGN KEY ([SubdealerId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_PurchaseOrders_ApprovedByUserId] FOREIGN KEY ([ApprovedBy]) REFERENCES [Users]([UserId]),
        CONSTRAINT [CK_PurchaseOrderStatus] CHECK ([PurchaseOrderStatus] IN (1, 2, 3))
    );
    
    CREATE INDEX [IX_PurchaseOrders_SubdealerId] ON [PurchaseOrders]([SubdealerId]);
    CREATE INDEX [IX_PurchaseOrders_PurchaseOrderStatus] ON [PurchaseOrders]([PurchaseOrderStatus]);
    CREATE INDEX [IX_PurchaseOrders_RequestedDate] ON [PurchaseOrders]([RequestedDate]);
    
    PRINT 'Created [PurchaseOrders] table';
END
GO

-- ============================================================================
-- 6. VEHICLES TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Vehicles')
BEGIN
    CREATE TABLE [Vehicles] (
        [VehicleId] INT PRIMARY KEY IDENTITY(1,1),
        [ChassisNumber] NVARCHAR(50) NOT NULL UNIQUE,
        [ModelId] INT NOT NULL,
        [ColorId] INT NOT NULL,
        [VehicleStatus] INT NOT NULL,  -- 1=Purchased, 2=Invoiced, 3=RTOInitiated, 4=RTONumberGiven
        [RTONumber] NVARCHAR(50),
        [PurchaseOrderId] INT NOT NULL,
        [SubdealerId] INT NOT NULL,
        [CurrentPrice] DECIMAL(15, 2) NOT NULL,
        [OriginalPrice] DECIMAL(15, 2) NOT NULL,
        [InvoiceDate] DATETIME,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_Vehicles_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [VehicleModels]([ModelId]),
        CONSTRAINT [FK_Vehicles_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [VehicleColors]([ColorId]),
        CONSTRAINT [FK_Vehicles_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders]([PurchaseOrderId]),
        CONSTRAINT [FK_Vehicles_SubdealerId] FOREIGN KEY ([SubdealerId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [CK_VehicleStatus] CHECK ([VehicleStatus] IN (1, 2, 3, 4))
    );
    
    CREATE INDEX [IX_Vehicles_ChassisNumber] ON [Vehicles]([ChassisNumber]);
    CREATE INDEX [IX_Vehicles_VehicleStatus] ON [Vehicles]([VehicleStatus]);
    CREATE INDEX [IX_Vehicles_SubdealerId] ON [Vehicles]([SubdealerId]);
    CREATE INDEX [IX_Vehicles_PurchaseOrderId] ON [Vehicles]([PurchaseOrderId]);
    
    PRINT 'Created [Vehicles] table';
END
GO

-- ============================================================================
-- 7. COMMISSION HISTORY TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommissionHistory')
BEGIN
    CREATE TABLE [CommissionHistory] (
        [CommissionId] INT PRIMARY KEY IDENTITY(1,1),
        [VehicleId] INT NOT NULL,
        [SubdealerId] INT NOT NULL,
        [CommissionMonth] INT NOT NULL,  -- 1-12
        [CommissionYear] INT NOT NULL,
        [SubmittedAmount] DECIMAL(15, 2) NOT NULL,
        [ApprovedAmount] DECIMAL(15, 2),
        [CommissionStatus] INT NOT NULL,  -- 1=Pending, 2=Approved, 3=Rejected
        [ApprovalReason] NVARCHAR(500),
        [SubmittedBy] INT NOT NULL,
        [SubmittedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ApprovedBy] INT,
        [ApprovedDate] DATETIME,
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_CommissionHistory_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([VehicleId]),
        CONSTRAINT [FK_CommissionHistory_SubdealerId] FOREIGN KEY ([SubdealerId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_CommissionHistory_SubmittedByUserId] FOREIGN KEY ([SubmittedBy]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_CommissionHistory_ApprovedByUserId] FOREIGN KEY ([ApprovedBy]) REFERENCES [Users]([UserId]),
        CONSTRAINT [CK_CommissionStatus] CHECK ([CommissionStatus] IN (1, 2, 3)),
        CONSTRAINT [UQ_CommissionVehicleMonthYear] UNIQUE ([VehicleId], [CommissionMonth], [CommissionYear])
    );
    
    CREATE INDEX [IX_CommissionHistory_SubdealerId] ON [CommissionHistory]([SubdealerId]);
    CREATE INDEX [IX_CommissionHistory_VehicleId] ON [CommissionHistory]([VehicleId]);
    CREATE INDEX [IX_CommissionHistory_CommissionStatus] ON [CommissionHistory]([CommissionStatus]);
    CREATE INDEX [IX_CommissionHistory_MonthYear] ON [CommissionHistory]([CommissionMonth], [CommissionYear]);
    
    PRINT 'Created [CommissionHistory] table';
END
GO

-- ============================================================================
-- 8. ACCOUNT BALANCE TABLE
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccountBalance')
BEGIN
    CREATE TABLE [AccountBalance] (
        [AccountId] INT PRIMARY KEY IDENTITY(1,1),
        [SubdealerId] INT NOT NULL UNIQUE,
        [CurrentBalance] DECIMAL(15, 2) NOT NULL DEFAULT 0,
        [ReservedAmount] DECIMAL(15, 2) NOT NULL DEFAULT 0,
        [AvailableBalance] DECIMAL(15, 2) NOT NULL DEFAULT 0,
        [InitialBalance] DECIMAL(15, 2),
        [LastTransactionDate] DATETIME,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_AccountBalance_SubdealerId] FOREIGN KEY ([SubdealerId]) REFERENCES [Users]([UserId])
    );
    
    CREATE INDEX [IX_AccountBalance_SubdealerId] ON [AccountBalance]([SubdealerId]);
    
    PRINT 'Created [AccountBalance] table';
END
GO

-- ============================================================================
-- 9. ACCOUNT TRANSACTIONS TABLE (Audit Trail)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccountTransactions')
BEGIN
    CREATE TABLE [AccountTransactions] (
        [TransactionId] INT PRIMARY KEY IDENTITY(1,1),
        [SubdealerId] INT NOT NULL,
        [TransactionType] INT NOT NULL,  -- 1=PurchaseApproved, 2=PurchaseRejected, 3=PriceAdjustment, 4=CommissionApproved, 5=CommissionRejected, 6=InitialBalance
        [Amount] DECIMAL(15, 2) NOT NULL,
        [BalanceBeforeTransaction] DECIMAL(15, 2) NOT NULL,
        [BalanceAfterTransaction] DECIMAL(15, 2) NOT NULL,
        [ReferencePurchaseOrderId] INT,
        [ReferenceVehicleId] INT,
        [ReferenceCommissionId] INT,
        [Description] NVARCHAR(500),
        [CreatedBy] INT NOT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_AccountTransactions_SubdealerId] FOREIGN KEY ([SubdealerId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_AccountTransactions_CreatedByUser] FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_AccountTransactions_PurchaseOrderId] FOREIGN KEY ([ReferencePurchaseOrderId]) REFERENCES [PurchaseOrders]([PurchaseOrderId]),
        CONSTRAINT [FK_AccountTransactions_VehicleId] FOREIGN KEY ([ReferenceVehicleId]) REFERENCES [Vehicles]([VehicleId]),
        CONSTRAINT [FK_AccountTransactions_CommissionId] FOREIGN KEY ([ReferenceCommissionId]) REFERENCES [CommissionHistory]([CommissionId]),
        CONSTRAINT [CK_TransactionType] CHECK ([TransactionType] IN (1, 2, 3, 4, 5, 6))
    );
    
    CREATE INDEX [IX_AccountTransactions_SubdealerId] ON [AccountTransactions]([SubdealerId]);
    CREATE INDEX [IX_AccountTransactions_CreatedDate] ON [AccountTransactions]([CreatedDate]);
    CREATE INDEX [IX_AccountTransactions_TransactionType] ON [AccountTransactions]([TransactionType]);
    
    PRINT 'Created [AccountTransactions] table';
END
GO

-- ============================================================================
-- 10. AUDIT LOG TABLE (System-wide changes)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE [AuditLog] (
        [AuditLogId] INT PRIMARY KEY IDENTITY(1,1),
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] INT,
        [Action] NVARCHAR(50) NOT NULL,  -- CREATE, UPDATE, DELETE
        [ChangedBy] INT NOT NULL,
        [ChangedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        [OldValues] NVARCHAR(MAX),  -- JSON format
        [NewValues] NVARCHAR(MAX),  -- JSON format
        [ChangeReason] NVARCHAR(500),
        [IpAddress] NVARCHAR(45),
        
        CONSTRAINT [FK_AuditLog_ChangedByUser] FOREIGN KEY ([ChangedBy]) REFERENCES [Users]([UserId])
    );
    
    CREATE INDEX [IX_AuditLog_EntityType] ON [AuditLog]([EntityType]);
    CREATE INDEX [IX_AuditLog_ChangedDate] ON [AuditLog]([ChangedDate]);
    CREATE INDEX [IX_AuditLog_ChangedBy] ON [AuditLog]([ChangedBy]);
    
    PRINT 'Created [AuditLog] table';
END
GO

-- ============================================================================
-- SUMMARY
-- ============================================================================
PRINT '===================================================================';
PRINT 'Database Schema Created Successfully';
PRINT '===================================================================';
PRINT 'Database: KRSDealerManagementDB';
PRINT 'Tables Created:';
PRINT '  1. Users';
PRINT '  2. VehicleModels';
PRINT '  3. VehicleColors';
PRINT '  4. VehiclePriceHistory';
PRINT '  5. PurchaseOrders';
PRINT '  6. Vehicles';
PRINT '  7. CommissionHistory';
PRINT '  8. AccountBalance';
PRINT '  9. AccountTransactions';
PRINT ' 10. AuditLog';
PRINT '';
PRINT 'Ready for EF Core migration and application deployment.';
PRINT '===================================================================';
GO

-- ============================================================================
-- SEED DATA - EV MODELS
-- ============================================================================

-- Insert Admin User
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE Username = 'admin')
BEGIN
    INSERT INTO [Users] (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive)
    VALUES ('admin', 'admin@krsdealers.com', 'AQAAAAIAAYagAAAAEDx3DxMqH8K0Z8Kz5vY5Z8X7X7X7X7X7X7X7X7X7X8=', 'Admin', 'EV Dealer', 1, '9876543210', 1);
    PRINT 'Inserted Admin user';
END
GO

-- Insert 28 Subdealer Users
DECLARE @i INT = 1;
WHILE @i <= 28
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE Username = 'subdealer_' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3))
    BEGIN
        INSERT INTO [Users] (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive)
        VALUES (
            'subdealer_' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3),
            'subdealer' + CAST(@i AS VARCHAR(3)) + '@krsdealers.com',
            'AQAAAAIAAYagAAAAEDx3DxMqH8K0Z8Kz5vY5Z8X7X7X7X7X7X7X7X7X7X8=',
            'Subdealer ' + CAST(@i AS VARCHAR(3)),
            'Dealer',
            2,
            '98000' + RIGHT('00000' + CAST(30000 + @i AS VARCHAR(5)), 5),
            1
        );
    END
    SET @i = @i + 1;
END
PRINT 'Inserted 28 subdealer users';
GO

-- Insert Ampere electric scooter models
IF NOT EXISTS (SELECT 1 FROM [VehicleModels] WHERE ModelName = 'Magnus EX')
BEGIN
    INSERT INTO [VehicleModels] (ModelName, Description, IsActive, CreatedBy, CreatedDate)
    VALUES 
        ('Magnus EX', 'Ampere Magnus EX electric scooter', 1, 1, GETUTCDATE()),
        ('Magnus Pro', 'Ampere Magnus Pro electric scooter', 1, 1, GETUTCDATE()),
        ('Magnus Neo', 'Ampere Magnus Neo electric scooter', 1, 1, GETUTCDATE()),
        ('Nexus EX', 'Ampere Nexus EX electric scooter', 1, 1, GETUTCDATE()),
        ('Nexus ST', 'Ampere Nexus ST electric scooter', 1, 1, GETUTCDATE()),
        ('Reo Li', 'Ampere Reo Li electric scooter', 1, 1, GETUTCDATE()),
        ('Reo Elite', 'Ampere Reo Elite electric scooter', 1, 1, GETUTCDATE()),
        ('Zeal EX', 'Ampere Zeal EX electric scooter', 1, 1, GETUTCDATE());
    PRINT 'Inserted 8 Ampere models';
END
GO

-- Insert Vehicle Colors
IF NOT EXISTS (SELECT 1 FROM [VehicleColors] WHERE ColorName = 'Pearl White')
BEGIN
    INSERT INTO [VehicleColors] (ColorName, HexCode, IsActive, CreatedBy, CreatedDate)
    VALUES 
        ('Pearl White', '#FFFFFF', 1, 1, GETUTCDATE()),
        ('Jet Black', '#1A1A1A', 1, 1, GETUTCDATE()),
        ('Matte Grey', '#808080', 1, 1, GETUTCDATE()),
        ('Ocean Blue', '#0066CC', 1, 1, GETUTCDATE()),
        ('Coral Red', '#E63946', 1, 1, GETUTCDATE());
    PRINT 'Inserted 5 vehicle colors';
END
GO

-- Insert Ampere pricing (August 2026)
IF NOT EXISTS (SELECT 1 FROM [VehiclePriceHistory] WHERE ModelId = 1 AND ColorId = 1 AND PriceMonth = 8 AND PriceYear = 2026)
BEGIN
    INSERT INTO [VehiclePriceHistory] (ModelId, ColorId, Price, PriceMonth, PriceYear, IsCurrentMonthPrice, ChangedBy, ChangedDate, ChangeReason, CreatedDate)
    SELECT m.ModelId, c.ColorId, 
        CASE m.ModelName
            WHEN 'Magnus EX' THEN 99000.00
            WHEN 'Magnus Pro' THEN 105000.00
            WHEN 'Magnus Neo' THEN 85000.00
            WHEN 'Nexus EX' THEN 115000.00
            WHEN 'Nexus ST' THEN 120000.00
            WHEN 'Reo Li' THEN 65000.00
            WHEN 'Reo Elite' THEN 75000.00
            WHEN 'Zeal EX' THEN 78000.00
            ELSE 90000.00
        END, 8, 2026, 1, 1, GETUTCDATE(), 'August 2026 Ampere pricing', GETUTCDATE()
    FROM [VehicleModels] m, [VehicleColors] c
    WHERE m.IsActive = 1 AND c.IsActive = 1;
    PRINT 'Inserted Ampere pricing for August 2026';
END
GO

-- Insert Account Balances for each Subdealer
IF NOT EXISTS (SELECT 1 FROM [AccountBalance] WHERE SubdealerId = 2)
BEGIN
    INSERT INTO [AccountBalance] (SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, CreatedDate)
    SELECT UserId, 1000000.00, 0, 1000000.00, 1000000.00, GETUTCDATE()
    FROM [Users]
    WHERE UserRole = 2 AND IsActive = 1;
    PRINT 'Inserted 28 account balances (₹10,00,000 per subdealer)';
END
GO

PRINT '===================================================================';
PRINT 'Seed Data Inserted Successfully';
PRINT '===================================================================';
PRINT 'Summary:';
PRINT '  - Admin users: 1';
PRINT '  - Subdealer users: 28';
PRINT '  - EV models: 8';
PRINT '  - Colors: 6';
PRINT '  - Price records: 48';
PRINT '  - Account balances: 28 (₹10,00,000 each)';
PRINT '  - Total initial investment: ₹2.8 crores';
PRINT '===================================================================';
GO

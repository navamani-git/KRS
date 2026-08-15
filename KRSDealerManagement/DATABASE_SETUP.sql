-- KRS Dealer Management System - Database Setup Script
-- Connection: localhost\SQLEXPRESS
-- Database: KRSDealerManagementDB
-- Run this entire script in SSMS

-- =====================================================
-- 1. CREATE DATABASE
-- =====================================================

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'KRSDealerManagementDB')
BEGIN
    ALTER DATABASE KRSDealerManagementDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE KRSDealerManagementDB;
END

CREATE DATABASE KRSDealerManagementDB;
GO

USE KRSDealerManagementDB;
GO

-- =====================================================
-- 2. CREATE TABLES
-- =====================================================

-- Users Table
CREATE TABLE [User] (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100),
    UserRole INT NOT NULL, -- 1=Admin, 2=Subdealer
    PhoneNumber NVARCHAR(20),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    INDEX IX_Username (Username),
    INDEX IX_UserRole (UserRole),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- SubdealerAccount Table
CREATE TABLE SubdealerAccount (
    AccountId INT PRIMARY KEY IDENTITY(1,1),
    SubdealerId INT NOT NULL,
    AccountName NVARCHAR(100) NOT NULL,
    AccountType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (SubdealerId) REFERENCES [User](UserId),
    INDEX IX_SubdealerId (SubdealerId),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- AccountPermission Table
CREATE TABLE AccountPermission (
    PermissionId INT PRIMARY KEY IDENTITY(1,1),
    AccountId INT NOT NULL,
    MenuKey NVARCHAR(50) NOT NULL,
    MenuName NVARCHAR(100) NOT NULL,
    IsAccessible BIT DEFAULT 0,
    CanCreate BIT DEFAULT 0,
    CanEdit BIT DEFAULT 0,
    CanDelete BIT DEFAULT 0,
    CanApprove BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (AccountId) REFERENCES SubdealerAccount(AccountId),
    INDEX IX_AccountId (AccountId),
    INDEX IX_MenuKey (MenuKey)
);

-- AccountBalance Table
CREATE TABLE AccountBalance (
    BalanceId INT PRIMARY KEY IDENTITY(1,1),
    SubdealerAccountId INT NOT NULL,
    SubdealerId INT NOT NULL,
    CurrentBalance DECIMAL(18,2) DEFAULT 0,
    ReservedAmount DECIMAL(18,2) DEFAULT 0,
    AvailableBalance DECIMAL(18,2) DEFAULT 0,
    InitialBalance DECIMAL(18,2),
    LastTransactionDate DATETIME,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    UNIQUE (SubdealerAccountId),
    FOREIGN KEY (SubdealerAccountId) REFERENCES SubdealerAccount(AccountId),
    FOREIGN KEY (SubdealerId) REFERENCES [User](UserId),
    INDEX IX_SubdealerId (SubdealerId),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- VehicleModel Table
CREATE TABLE VehicleModel (
    ModelId INT PRIMARY KEY IDENTITY(1,1),
    ModelName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedBy INT,
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [User](UserId),
    INDEX IX_ModelName (ModelName),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- VehicleColor Table
CREATE TABLE VehicleColor (
    ColorId INT PRIMARY KEY IDENTITY(1,1),
    ColorName NVARCHAR(50) NOT NULL UNIQUE,
    HexCode NVARCHAR(7),
    IsActive BIT DEFAULT 1,
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedBy INT,
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [User](UserId),
    INDEX IX_ColorName (ColorName)
);

-- VehiclePriceHistory Table
CREATE TABLE VehiclePriceHistory (
    PriceHistoryId INT PRIMARY KEY IDENTITY(1,1),
    VehicleId INT,
    ModelId INT NOT NULL,
    ColorId INT NOT NULL,
    Month INT NOT NULL, -- 1-12
    Year INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Notes NVARCHAR(500),
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedBy INT,
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (ModelId) REFERENCES VehicleModel(ModelId),
    FOREIGN KEY (ColorId) REFERENCES VehicleColor(ColorId),
    FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [User](UserId),
    INDEX IX_ModelIdColorId (ModelId, ColorId),
    INDEX IX_MonthYear (Month, Year),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- Vehicle Table
CREATE TABLE Vehicle (
    VehicleId INT PRIMARY KEY IDENTITY(1,1),
    ModelId INT NOT NULL,
    ColorId INT NOT NULL,
    ChassisNumber NVARCHAR(20) NOT NULL UNIQUE,
    Status INT DEFAULT 0, -- 0=Available, 1=Reserved, 2=Sold, 3=Damaged
    ManufacturingYear INT,
    RegistrationNumber NVARCHAR(20),
    StockLocation NVARCHAR(100),
    Notes NVARCHAR(500),
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedBy INT,
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (ModelId) REFERENCES VehicleModel(ModelId),
    FOREIGN KEY (ColorId) REFERENCES VehicleColor(ColorId),
    FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [User](UserId),
    INDEX IX_ChassisNumber (ChassisNumber),
    INDEX IX_Status (Status),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- PurchaseOrder Table
CREATE TABLE PurchaseOrder (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    AccountId INT NOT NULL,
    SubdealerId INT NOT NULL,
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    TotalQuantity INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status INT DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected, 3=Delivered
    AdminNotes NVARCHAR(500),
    SubdealerNotes NVARCHAR(500),
    ApprovedBy INT,
    ApprovedDate DATETIME,
    DeliveryDate DATETIME,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (AccountId) REFERENCES SubdealerAccount(AccountId),
    FOREIGN KEY (SubdealerId) REFERENCES [User](UserId),
    FOREIGN KEY (ApprovedBy) REFERENCES [User](UserId),
    INDEX IX_OrderNumber (OrderNumber),
    INDEX IX_Status (Status),
    INDEX IX_SubdealerId (SubdealerId),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- Commission Table
CREATE TABLE Commission (
    CommissionId INT PRIMARY KEY IDENTITY(1,1),
    AccountId INT NOT NULL,
    SubdealerId INT NOT NULL,
    VehicleId INT NOT NULL,
    Month INT NOT NULL, -- 1-12
    Year INT NOT NULL,
    CommissionAmount DECIMAL(18,2) NOT NULL,
    Status INT DEFAULT 0, -- 0=Pending, 1=Approved, 2=Paid, 3=Rejected
    Notes NVARCHAR(500),
    ApprovedBy INT,
    ApprovedDate DATETIME,
    PaidDate DATETIME,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (AccountId) REFERENCES SubdealerAccount(AccountId),
    FOREIGN KEY (SubdealerId) REFERENCES [User](UserId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId),
    FOREIGN KEY (ApprovedBy) REFERENCES [User](UserId),
    INDEX IX_MonthYear (Month, Year),
    INDEX IX_Status (Status),
    INDEX IX_SubdealerId (SubdealerId),
    INDEX IX_VehicleId (VehicleId),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- CommissionRate Table
CREATE TABLE CommissionRate (
    CommissionRateId INT PRIMARY KEY IDENTITY(1,1),
    ModelId INT NOT NULL,
    CommissionAmount DECIMAL(18,2) NOT NULL,
    StartMonth INT NOT NULL, -- 1-12
    StartYear INT NOT NULL,
    ExpiryMonth INT,
    ExpiryYear INT,
    Notes NVARCHAR(500),
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedBy INT,
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (ModelId) REFERENCES VehicleModel(ModelId),
    FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    FOREIGN KEY (ModifiedBy) REFERENCES [User](UserId),
    INDEX IX_ModelId (ModelId),
    INDEX IX_StartMonthYear (StartMonth, StartYear),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- ReturnRequest Table
CREATE TABLE ReturnRequest (
    ReturnRequestId INT PRIMARY KEY IDENTITY(1,1),
    AccountId INT NOT NULL,
    OrderId INT NOT NULL,
    VehicleId INT NOT NULL,
    RefundAmount DECIMAL(18,2) NOT NULL,
    Status INT DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected
    ReturnReason NVARCHAR(500),
    AdminRemarks NVARCHAR(500),
    ProcessedBy INT,
    ProcessedDate DATETIME,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (AccountId) REFERENCES SubdealerAccount(AccountId),
    FOREIGN KEY (OrderId) REFERENCES PurchaseOrder(OrderId),
    FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId),
    FOREIGN KEY (ProcessedBy) REFERENCES [User](UserId),
    INDEX IX_Status (Status),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- Payment Table
CREATE TABLE Payment (
    PaymentId INT PRIMARY KEY IDENTITY(1,1),
    AccountId INT NOT NULL,
    SubdealerId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentType NVARCHAR(50) NOT NULL, -- Cash, GPay, NEFT, Others
    PaymentDate DATETIME NOT NULL,
    Status INT DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected
    SubdealerRemarks NVARCHAR(500),
    DealerRemarks NVARCHAR(500),
    ProcessedBy INT,
    ProcessedDate DATETIME,
    IsApplied BIT DEFAULT 0,
    TransactionId INT,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (AccountId) REFERENCES SubdealerAccount(AccountId),
    FOREIGN KEY (SubdealerId) REFERENCES [User](UserId),
    FOREIGN KEY (ProcessedBy) REFERENCES [User](UserId),
    INDEX IX_Status (Status),
    INDEX IX_SubdealerId (SubdealerId),
    INDEX IX_CreatedDate (CreatedDate DESC)
);

-- AccountTransaction Table
CREATE TABLE AccountTransaction (
    TransactionId INT PRIMARY KEY IDENTITY(1,1),
    AccountId INT NOT NULL,
    TransactionType INT NOT NULL, -- 1=Debit, 2=Credit
    Amount DECIMAL(18,2) NOT NULL,
    BalanceAfterTransaction DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(200) NOT NULL,
    ReferenceId INT,
    ReferenceType NVARCHAR(50),
    Remarks NVARCHAR(500),
    InitiatedBy INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (AccountId) REFERENCES SubdealerAccount(AccountId),
    FOREIGN KEY (InitiatedBy) REFERENCES [User](UserId),
    INDEX IX_AccountId (AccountId),
    INDEX IX_CreatedDate (CreatedDate DESC),
    INDEX IX_ReferenceType (ReferenceType)
);

-- AuditLog Table
CREATE TABLE AuditLog (
    AuditLogId INT PRIMARY KEY IDENTITY(1,1),
    EntityType NVARCHAR(50) NOT NULL,
    EntityId INT NOT NULL,
    Action NVARCHAR(50) NOT NULL, -- Create, Update, Delete, Approve, Reject
    UserId INT NOT NULL,
    UserRole NVARCHAR(50),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    Remarks NVARCHAR(1000),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    INDEX IX_EntityTypeId (EntityType, EntityId),
    INDEX IX_CreatedDate (CreatedDate DESC),
    INDEX IX_UserId (UserId)
);

-- =====================================================
-- 4. SEED DATA
-- =====================================================

-- Seed Admin User
INSERT INTO [User] (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive)
VALUES ('admin', 'admin@krsdealers.com', 'AQAAAAIAAYagAAAAEDx3DxMqH8K0Z8Kz5vY5Z8X7X7X7X7X7X7X7X7X7X8=', 'Admin', 'EV Dealer', 1, '9876543210', 1);

-- Seed 28 Subdealers
DECLARE @i INT = 1;
WHILE @i <= 28
BEGIN
    INSERT INTO [User] (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive)
    VALUES (
        'subdealer_' + RIGHT('000' + CAST(@i AS VARCHAR(3)), 3),
        'subdealer' + CAST(@i AS VARCHAR(3)) + '@krsdealers.com',
        'AQAAAAIAAYagAAAAEDx3DxMqH8K0Z8Kz5vY5Z8X7X7X7X7X7X7X7X7X7X8=', -- Default password hash
        'Subdealer ' + CAST(@i AS VARCHAR(3)),
        'Dealer',
        2,
        '98000' + RIGHT('00000' + CAST(30000 + @i AS VARCHAR(5)), 5),
        1
    );
    SET @i = @i + 1;
END;

-- Seed Vehicle Models (Ampere electric scooters only)
INSERT INTO VehicleModel (ModelName, Description, IsActive, CreatedBy, CreatedDate)
VALUES 
    ('Magnus EX', 'Ampere Magnus EX electric scooter', 1, 1, GETUTCDATE()),
    ('Magnus Pro', 'Ampere Magnus Pro electric scooter', 1, 1, GETUTCDATE()),
    ('Magnus Neo', 'Ampere Magnus Neo electric scooter', 1, 1, GETUTCDATE()),
    ('Nexus EX', 'Ampere Nexus EX electric scooter', 1, 1, GETUTCDATE()),
    ('Nexus ST', 'Ampere Nexus ST electric scooter', 1, 1, GETUTCDATE()),
    ('Reo Li', 'Ampere Reo Li electric scooter', 1, 1, GETUTCDATE()),
    ('Reo Elite', 'Ampere Reo Elite electric scooter', 1, 1, GETUTCDATE()),
    ('Zeal EX', 'Ampere Zeal EX electric scooter', 1, 1, GETUTCDATE());

-- Seed Vehicle Colors
INSERT INTO VehicleColor (ColorName, HexCode, IsActive, CreatedBy, CreatedDate)
VALUES 
    ('Pearl White', '#FFFFFF', 1, 1, GETUTCDATE()),
    ('Jet Black', '#1A1A1A', 1, 1, GETUTCDATE()),
    ('Matte Grey', '#808080', 1, 1, GETUTCDATE()),
    ('Ocean Blue', '#0066CC', 1, 1, GETUTCDATE()),
    ('Coral Red', '#E63946', 1, 1, GETUTCDATE());

-- Seed Commission Rates (for August 2026 - Ampere scooters)
INSERT INTO CommissionRate (ModelId, CommissionAmount, StartMonth, StartYear, ExpiryMonth, ExpiryYear, Notes, CreatedBy, CreatedDate)
VALUES 
    (1, 2800.00, 8, 2026, NULL, NULL, 'Ampere Magnus EX commission', 1, GETUTCDATE()),
    (2, 3000.00, 8, 2026, NULL, NULL, 'Ampere Magnus Pro commission', 1, GETUTCDATE()),
    (3, 2500.00, 8, 2026, NULL, NULL, 'Ampere Magnus Neo commission', 1, GETUTCDATE()),
    (4, 3200.00, 8, 2026, NULL, NULL, 'Ampere Nexus EX commission', 1, GETUTCDATE()),
    (5, 3500.00, 8, 2026, NULL, NULL, 'Ampere Nexus ST commission', 1, GETUTCDATE()),
    (6, 2000.00, 8, 2026, NULL, NULL, 'Ampere Reo Li commission', 1, GETUTCDATE()),
    (7, 2200.00, 8, 2026, NULL, NULL, 'Ampere Reo Elite commission', 1, GETUTCDATE()),
    (8, 2300.00, 8, 2026, NULL, NULL, 'Ampere Zeal EX commission', 1, GETUTCDATE());

-- Seed Vehicle Prices (for August 2026 - Ampere ex-showroom pricing in INR)
INSERT INTO VehiclePriceHistory (ModelId, ColorId, Month, Year, Price, Notes, CreatedBy, CreatedDate)
SELECT m.ModelId, c.ColorId, 8, 2026, 
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
    END, 'August 2026 Ampere pricing', 1, GETUTCDATE()
FROM VehicleModel m, VehicleColor c
WHERE m.IsActive = 1 AND c.IsActive = 1;

-- Create SubdealerAccount for each Subdealer (Main account)
INSERT INTO SubdealerAccount (SubdealerId, AccountName, AccountType, Description, IsActive, CreatedDate)
SELECT UserId, 'Main Account', 'Sales', 'Primary sales account', 1, GETUTCDATE()
FROM [User]
WHERE UserRole = 2;

-- Create AccountBalance for each SubdealerAccount
INSERT INTO AccountBalance (SubdealerAccountId, SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, CreatedDate)
SELECT AccountId, SubdealerId, 1000000.00, 0, 1000000.00, 1000000.00, GETUTCDATE()
FROM SubdealerAccount
WHERE IsActive = 1;

-- Create Default Permissions for each account
INSERT INTO AccountPermission (AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove, CreatedDate)
SELECT AccountId, 'purchase_orders', 'Purchase Orders', 1, 1, 1, 0, 0, GETUTCDATE()
FROM SubdealerAccount
WHERE IsActive = 1
UNION ALL
SELECT AccountId, 'commissions', 'Commissions', 1, 1, 1, 0, 0, GETUTCDATE()
FROM SubdealerAccount
WHERE IsActive = 1
UNION ALL
SELECT AccountId, 'payments', 'Payments', 1, 1, 1, 0, 0, GETUTCDATE()
FROM SubdealerAccount
WHERE IsActive = 1
UNION ALL
SELECT AccountId, 'account_details', 'Account Details', 1, 0, 0, 0, 0, GETUTCDATE()
FROM SubdealerAccount
WHERE IsActive = 1;

-- =====================================================
-- 4. VERIFICATION
-- =====================================================

PRINT '=== Database Setup Complete ===';
PRINT 'Total Users: ' + CAST((SELECT COUNT(*) FROM [User]) AS VARCHAR(10));
PRINT 'Total SubdealerAccounts: ' + CAST((SELECT COUNT(*) FROM SubdealerAccount) AS VARCHAR(10));
PRINT 'Total VehicleModels: ' + CAST((SELECT COUNT(*) FROM VehicleModel) AS VARCHAR(10));
PRINT 'Total VehicleColors: ' + CAST((SELECT COUNT(*) FROM VehicleColor) AS VARCHAR(10));
PRINT 'Total CommissionRates: ' + CAST((SELECT COUNT(*) FROM CommissionRate) AS VARCHAR(10));
PRINT 'Total Permissions: ' + CAST((SELECT COUNT(*) FROM AccountPermission) AS VARCHAR(10));
PRINT '';
PRINT 'Admin Credentials:';
PRINT 'Username: admin';
PRINT 'Password: Admin@123 (hash provided above)';
PRINT '';
PRINT 'Subdealer Credentials (28 total):';
PRINT 'Username: subdealer_001 to subdealer_028';
PRINT 'Password: Subdealers@123 (hash provided above)';
PRINT '';
PRINT 'Default Balance per Subdealer: ₹10,00,000';

GO

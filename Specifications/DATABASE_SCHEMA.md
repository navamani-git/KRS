# Vehicle Dealer Management System - Database Schema Design

**Version:** 1.0  
**Date:** August 7, 2026  
**Database:** SQL Server  
**ORM:** Entity Framework Core

---

## 1. Database Diagram & Entity Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                        CORE ENTITIES                            │
├─────────────────────────────────────────────────────────────────┤
│
│  User (Admin, Subdealers)
│  ├── UserRole (enum: Admin, Subdealers)
│  └── Relationships: AccountBalance, PurchaseOrders, Commissions
│
│  Vehicle Model & Color
│  ├── VehicleModel (e.g., BMW, Toyota)
│  └── VehicleColor (e.g., White, Black)
│
│  Vehicle (Individual instances with Chassis Numbers)
│  ├── VehicleStatus (Purchased, Invoiced, RTOInitiated, RTONumberGiven)
│  ├── Relationships: PurchaseOrder, VehiclePrice, Commission
│  └── Audit: CreatedBy, CreatedDate, ModifiedDate
│
│  Purchase Order Management
│  ├── PurchaseOrder (Request from subdealers)
│  ├── PurchaseOrderStatus (Pending, Approved, Rejected)
│  └── Relationships: User, Vehicle
│
│  Pricing & Commission
│  ├── VehiclePriceHistory (Monthly pricing with history)
│  ├── CommissionHistory (Monthly commission submissions)
│  └── Audit: ChangedBy, ChangedDate, Reason
│
│  Account Management
│  ├── AccountBalance (Current balance per subdealers)
│  └── AccountTransaction (Complete audit trail of all movements)
│
│  Audit Trail
│  └── AuditLog (All system changes: Who, What, When, Why)
│
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Detailed Table Schemas

### 2.1 Users Table
```sql
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

-- Indexes
CREATE INDEX [IX_Users_Username] ON [Users]([Username]);
CREATE INDEX [IX_Users_UserRole] ON [Users]([UserRole]);
CREATE INDEX [IX_Users_IsActive] ON [Users]([IsActive]);
```

**Enum Mapping:**
- UserRole: 1 = Admin, 2 = Subdealers

---

### 2.2 Vehicle Models Table
```sql
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
```

---

### 2.3 Vehicle Colors Table
```sql
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
```

---

### 2.4 Vehicle Price History Table
```sql
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
```

---

### 2.5 Vehicles Table
```sql
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
```

**Enum Mapping:**
- VehicleStatus: 1 = Purchased, 2 = Invoiced, 3 = RTOInitiated, 4 = RTONumberGiven

---

### 2.6 Purchase Orders Table
```sql
CREATE TABLE [PurchaseOrders] (
    [PurchaseOrderId] INT PRIMARY KEY IDENTITY(1,1),
    [OrderNumber] NVARCHAR(50) NOT NULL UNIQUE,  -- e.g., PO-20260807-001
    [SubdealerId] INT NOT NULL,
    [TotalAmount] DECIMAL(15, 2) NOT NULL,
    [ApprovedAmount] DECIMAL(15, 2),  -- Amount approved by admin (may be less than total)
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
```

**Enum Mapping:**
- PurchaseOrderStatus: 1 = Pending, 2 = Approved, 3 = Rejected

---

### 2.7 Commission History Table
```sql
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
```

**Enum Mapping:**
- CommissionStatus: 1 = Pending, 2 = Approved, 3 = Rejected

---

### 2.8 Account Balance Table
```sql
CREATE TABLE [AccountBalance] (
    [AccountId] INT PRIMARY KEY IDENTITY(1,1),
    [SubdealerId] INT NOT NULL UNIQUE,
    [CurrentBalance] DECIMAL(15, 2) NOT NULL DEFAULT 0,
    [ReservedAmount] DECIMAL(15, 2) NOT NULL DEFAULT 0,  -- For pending purchase orders
    [AvailableBalance] DECIMAL(15, 2) NOT NULL DEFAULT 0,  -- CurrentBalance - ReservedAmount
    [InitialBalance] DECIMAL(15, 2),
    [LastTransactionDate] DATETIME,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_AccountBalance_SubdealerId] FOREIGN KEY ([SubdealerId]) REFERENCES [Users]([UserId])
);

CREATE INDEX [IX_AccountBalance_SubdealerId] ON [AccountBalance]([SubdealerId]);
```

---

### 2.9 Account Transactions Table (Audit Trail)
```sql
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
```

**Enum Mapping:**
- TransactionType: 1 = PurchaseApproved, 2 = PurchaseRejected, 3 = PriceAdjustment, 4 = CommissionApproved, 5 = CommissionRejected, 6 = InitialBalance

---

### 2.10 Audit Log Table (System-wide changes)
```sql
CREATE TABLE [AuditLog] (
    [AuditLogId] INT PRIMARY KEY IDENTITY(1,1),
    [EntityType] NVARCHAR(100) NOT NULL,  -- e.g., "Vehicle", "PurchaseOrder", "Commission"
    [EntityId] INT,
    [Action] NVARCHAR(50) NOT NULL,  -- "CREATE", "UPDATE", "DELETE"
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
```

---

## 3. Key Business Logic Rules

### 3.1 Price Management
- When a price is set for a month: Create entry in `VehiclePriceHistory`
- When retrieving price for a vehicle: Use latest price in current month, fallback to previous month
- When price changes for existing vehicle (before invoicing):
  - Calculate difference: `CurrentPrice - NewPrice`
  - Create `AccountTransaction` record
  - Update `Vehicle.CurrentPrice`
  - Update `AccountBalance.CurrentBalance`

### 3.2 Purchase Order Flow
1. **Create PurchaseOrder:**
   - Calculate TotalAmount based on current prices
   - Validate: `AccountBalance.AvailableBalance >= TotalAmount`
   - Reserve amount: `ReservedAmount += TotalAmount`
   
2. **Approve PurchaseOrder:**
   - Create Vehicle records with ChassisNumbers
   - Debit account: `CurrentBalance -= ApprovedAmount`
   - Release rejected vehicles: `ReservedAmount -= RejectedAmount`
   - Create AccountTransaction records

3. **Reject PurchaseOrder:**
   - Release reserved amount: `ReservedAmount -= TotalAmount`
   - No balance change
   - Create AccountTransaction record

### 3.3 Commission Processing
1. **Submit Commission:**
   - Status = Pending
   - Store SubmittedAmount
   
2. **Approve Commission:**
   - Status = Approved
   - ApprovedAmount set
   - Credit account: `CurrentBalance += ApprovedAmount`
   - Create AccountTransaction record

3. **Fallback Logic:**
   - Query latest commission for vehicle in previous months
   - Use that amount if current month not found

### 3.4 Account Balance Calculations
```
AvailableBalance = CurrentBalance - ReservedAmount
```

---

## 4. Database Initialization Scripts

### 4.1 Create Lookup Data (Enums)
```sql
-- These would be created during EF Core migrations
-- Users: Admin role setup
-- VehicleModels: Pre-populate with common models
-- VehicleColors: Pre-populate with standard colors
```

### 4.2 Sample Data for Testing
```sql
-- Insert test Admin user
-- Insert test Subdealers users (28 users)
-- Insert test vehicle models and colors
-- Create sample prices for current and previous months
-- Set initial balances for subdealers
```

---

## 5. Data Integrity & Constraints

| Constraint | Purpose |
|-----------|---------|
| Unique ChassisNumber | Prevent duplicate vehicles |
| Unique (ModelId, ColorId, Month, Year) in VehiclePriceHistory | One price per model/color/month |
| Unique (VehicleId, Month, Year) in CommissionHistory | One commission per vehicle/month |
| Foreign Keys | Maintain referential integrity |
| Check Constraints | Validate enum values |
| Not Null Constraints | Enforce required fields |

---

## 6. Performance Considerations

### Indexes Created For:
- **Frequent Queries:** User authentication, vehicle status lookups, purchase order filtering
- **Audit Trail:** Date range queries for transaction history
- **Relationships:** Foreign key lookups
- **Calculations:** Account balance queries by subdealers

### Query Optimization:
- Indexed columns for WHERE clauses
- Composite indexes for multi-field searches
- Separate price history table to avoid bloat in main vehicle table

---

## 7. Migration Strategy

**EF Core Code-First Approach:**
1. Define all entity models in C#
2. Create initial migration
3. Apply migrations to database
4. Seed initial data (users, models, colors)

---

## 8. Security & Compliance

- All dates stored in UTC (GETUTCDATE())
- Password hashes never stored in plaintext
- Complete audit trail for compliance
- Foreign key constraints ensure data integrity
- No direct access to sensitive data without authorization

---

**Schema Version:** 1.0  
**Created:** August 7, 2026  
**Ready for EF Core Implementation**

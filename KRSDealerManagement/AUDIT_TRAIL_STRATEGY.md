# Audit Trail Strategy - Complete Change Tracking

## Overview
Every screen change must be traceable: **WHO changed WHAT, WHEN, and WHY**

## Audit Scope

### Screen Changes Tracked
1. **Create Operations** - New records creation
2. **Update Operations** - Field modifications
3. **Delete Operations** - Record deletions (soft delete with reason)
4. **Approve/Reject Operations** - Status changes with remarks
5. **Status Transitions** - Order/Commission state changes
6. **Permission Changes** - Account permission modifications
7. **Balance Adjustments** - Manual balance changes
8. **Report Exports** - Who downloaded/exported what, when

---

## Audit Entities

### AuditLog Entity (Complete)
```
AuditLogId (PK)
EntityType: string (e.g., "PurchaseOrder", "Commission", "VehicleModel")
EntityId: int (ID of affected record)
Action: string (Create, Update, Delete, Approve, Reject, Restore, Export)
UserId: int (WHO - User performing action)
UserRole: string (Admin, Subdealer, Dealer - WHO's role)
OldValue: string (JSON - previous state)
NewValue: string (JSON - new state)
Remarks: string (WHY - reason for change)
IpAddress: string (Source of change)
UserAgent: string (Browser/device info)
CreatedDate: DateTime (WHEN - UTC timestamp)
```

### AccountTransaction Entity (For Balance Tracking)
```
TransactionId (PK)
AccountId: int (Which account)
TransactionType: int (1=Debit, 2=Credit)
Amount: decimal (How much)
BalanceAfterTransaction: decimal (Balance snapshot)
Reason: string (WHY - reason for transaction)
ReferenceId: int (Link to PurchaseOrder, Commission, etc.)
ReferenceType: string (PurchaseOrder, Commission, Return, Payment)
Remarks: string (Additional context)
InitiatedBy: int (WHO initiated)
CreatedDate: DateTime (WHEN)
```

---

## Tracking Per Screen/Feature

### Admin Screens

#### 1. Vehicle Model Management
**Audits:**
- Create VehicleModel → AuditLog (Action: "Create")
- Update VehicleModel (name, description) → AuditLog (Action: "Update", OldValue, NewValue)
- Deactivate Model → AuditLog (Action: "Update", OldValue: IsActive=true, NewValue: IsActive=false, Remarks)

**Example AuditLog:**
```json
{
  "EntityType": "VehicleModel",
  "EntityId": 5,
  "Action": "Create",
  "UserId": 1,
  "UserRole": "Admin",
  "OldValue": null,
  "NewValue": "{\"ModelId\":5,\"ModelName\":\"BMW X5\",\"Description\":\"Luxury SUV\"}",
  "Remarks": null,
  "CreatedDate": "2024-08-07 10:15:30"
}
```

#### 2. Vehicle Price Management
**Audits:**
- Create Price Record → AuditLog (Action: "Create", includes model, color, price, month, year)
- Update Price → AuditLog (Action: "Update", OldValue: old price, NewValue: new price, Remarks: reason)
- Delete/Deactivate Price → AuditLog (Action: "Delete", Remarks: reason)

**Example:**
```json
{
  "EntityType": "VehiclePriceHistory",
  "EntityId": 245,
  "Action": "Update",
  "UserId": 1,
  "UserRole": "Admin",
  "OldValue": "{\"Month\":8,\"Year\":2024,\"Price\":100000}",
  "NewValue": "{\"Month\":8,\"Year\":2024,\"Price\":102000}",
  "Remarks": "Price increase due to inflation",
  "IpAddress": "192.168.1.100",
  "CreatedDate": "2024-08-07 14:45:22"
}
```

#### 3. Commission Rate Management
**Audits:**
- Create Rate → AuditLog (model, amount, effective dates)
- Update Rate → AuditLog (old vs new amount, dates, remarks)
- Archive Rate → AuditLog (reason for archiving)

**Example:**
```json
{
  "EntityType": "CommissionRate",
  "EntityId": 12,
  "Action": "Create",
  "UserId": 1,
  "UserRole": "Admin",
  "NewValue": "{\"ModelId\":3,\"CommissionAmount\":5000,\"StartMonth\":8,\"StartYear\":2024}",
  "Remarks": "New BMW commission rate",
  "CreatedDate": "2024-08-07 09:30:00"
}
```

#### 4. Create Subdealer
**Audits:**
- Create Subdealer (User) → AuditLog (name, location, phones, username)
- Create SubdealerAccount → AuditLog (account name, type)
- Create AccountBalance → AuditLog (initial balance set)
- Create Default Permissions → AuditLog (permissions granted)

**Example:**
```json
{
  "EntityType": "User",
  "EntityId": 150,
  "Action": "Create",
  "UserId": 1,
  "UserRole": "Admin",
  "NewValue": "{\"Username\":\"subdealer_150\",\"FirstName\":\"Raj Patel\",\"Location\":\"Mumbai\"}",
  "Remarks": "New subdealer registration",
  "CreatedDate": "2024-08-07 11:20:15"
}
```

#### 5. Purchase Order Approval/Rejection
**Audits:**
- Approve Individual Vehicle → AuditLog (Action: "Approve", OrderId, VehicleId, amount, ApprovedBy, Remarks)
- Reject Individual Vehicle → AuditLog (Action: "Reject", OrderId, VehicleId, amount, Remarks)
- Also: AccountTransaction (Debit when approved, nothing when rejected but reserved amount released)

**Example Approval:**
```json
{
  "EntityType": "PurchaseOrder",
  "EntityId": 1001,
  "Action": "Approve",
  "UserId": 1,
  "UserRole": "Admin",
  "OldValue": "{\"Status\":\"Pending\"}",
  "NewValue": "{\"Status\":\"Approved\",\"ApprovedBy\":1,\"ApprovedDate\":\"2024-08-07T15:30:00Z\"}",
  "Remarks": "Approved after verification",
  "CreatedDate": "2024-08-07 15:30:45"
}
```

**Example Transaction:**
```json
{
  "TransactionId": 5001,
  "AccountId": 50,
  "TransactionType": 1, // Debit
  "Amount": 500000,
  "BalanceAfterTransaction": 4500000,
  "Reason": "Purchase Order Approval",
  "ReferenceId": 1001,
  "ReferenceType": "PurchaseOrder",
  "Remarks": "Approved 5 vehicles @ 100000 each",
  "InitiatedBy": 1,
  "CreatedDate": "2024-08-07 15:30:45"
}
```

#### 6. Return Request Approval
**Audits:**
- Create Return Request → AuditLog (Action: "Create")
- Approve Return → AuditLog (Action: "Approve", amount, remarks)
- Reject Return → AuditLog (Action: "Reject", reason)
- Also: AccountTransaction (Credit when approved)

**Example:**
```json
{
  "EntityType": "ReturnRequest",
  "EntityId": 101,
  "Action": "Approve",
  "UserId": 1,
  "UserRole": "Admin",
  "OldValue": "{\"Status\":\"Pending\"}",
  "NewValue": "{\"Status\":\"Approved\",\"ProcessedBy\":1,\"ProcessedDate\":\"2024-08-07T16:00:00Z\"}",
  "Remarks": "Vehicle returned in good condition",
  "CreatedDate": "2024-08-07 16:00:30"
}
```

---

### Subdealer Screens

#### 1. Create Purchase Order
**Audits:**
- Create Order → AuditLog (Action: "Create", items, total amount)
- Update Order (before approval) → AuditLog (Action: "Update", items changed)
- Cancel Order → AuditLog (Action: "Cancel", reason)
- Also: AccountTransaction (Reserve - no debit yet, just reserve)

**Example:**
```json
{
  "EntityType": "PurchaseOrder",
  "EntityId": 2001,
  "Action": "Create",
  "UserId": 150,
  "UserRole": "Subdealer",
  "NewValue": "{\"OrderNumber\":\"ORD-2024-2001\",\"AccountId\":50,\"TotalQuantity\":5,\"TotalAmount\":500000}",
  "Remarks": null,
  "CreatedDate": "2024-08-07 12:00:00"
}
```

**Reserved Amount Transaction:**
```json
{
  "TransactionId": 5002,
  "AccountId": 50,
  "TransactionType": 1, // Debit (Reserve)
  "Amount": 500000,
  "BalanceAfterTransaction": 4500000, // Available balance
  "Reason": "Purchase Order Created - Amount Reserved",
  "ReferenceId": 2001,
  "ReferenceType": "PurchaseOrder",
  "Remarks": "5 vehicles reserved pending approval",
  "InitiatedBy": 150,
  "CreatedDate": "2024-08-07 12:00:00"
}
```

#### 2. Submit Commission
**Audits:**
- Submit Commission → AuditLog (Action: "Create", VehicleId, Month, Amount)
- Update Commission (before approval) → AuditLog (Action: "Update")
- Cancel Commission → AuditLog (Action: "Cancel", reason)

**Example:**
```json
{
  "EntityType": "Commission",
  "EntityId": 3001,
  "Action": "Create",
  "UserId": 150,
  "UserRole": "Subdealer",
  "NewValue": "{\"VehicleId\":100,\"Month\":8,\"Year\":2024,\"CommissionAmount\":5000}",
  "Remarks": null,
  "CreatedDate": "2024-08-07 13:15:00"
}
```

#### 3. Submit Payment
**Audits:**
- Create Payment → AuditLog (amount, type, date, remarks)
- Update Payment → AuditLog (if modifiable before approval)
- Cancel Payment → AuditLog (reason)

**Example:**
```json
{
  "EntityType": "Payment",
  "EntityId": 4001,
  "Action": "Create",
  "UserId": 150,
  "UserRole": "Subdealer",
  "NewValue": "{\"Amount\":100000,\"PaymentType\":\"NEFT\",\"PaymentDate\":\"2024-08-07\"}",
  "Remarks": "Payment for August settlement",
  "CreatedDate": "2024-08-07 14:00:00"
}
```

#### 4. Request Return
**Audits:**
- Create Return Request → AuditLog (which vehicle, reason)

**Example:**
```json
{
  "EntityType": "ReturnRequest",
  "EntityId": 102,
  "Action": "Create",
  "UserId": 150,
  "UserRole": "Subdealer",
  "NewValue": "{\"VehicleId\":100,\"OrderId\":2001,\"Amount\":100000}",
  "Remarks": "Vehicle has mechanical issue",
  "CreatedDate": "2024-08-07 15:00:00"
}
```

---

### Dealer Screens

#### 1. Approve Individual Purchase Order Items
**Audits:**
- Approve Item → AuditLog (Action: "Approve", VehicleId, Amount, ApprovedBy, Remarks)
- Reject Item → AuditLog (Action: "Reject", VehicleId, Amount, Remarks)
- Also: AccountTransaction (Debit on approve, or just release reservation on reject)

#### 2. Approve Payment
**Audits:**
- Approve Payment → AuditLog (Action: "Approve", Amount, ApprovedBy, Remarks)
- Reject Payment → AuditLog (Action: "Reject", Reason)
- Also: AccountTransaction (optional - only if applied to balance)

#### 3. Approve Return Request
**Audits:**
- Approve Return → AuditLog (Action: "Approve", Amount, ApprovedBy)
- Reject Return → AuditLog (Action: "Reject", Reason)
- Also: AccountTransaction (Credit)

---

## Database Schema for Auditing

### AuditLog Table
```sql
CREATE TABLE AuditLog (
    AuditLogId INT PRIMARY KEY IDENTITY(1,1),
    EntityType NVARCHAR(50) NOT NULL,
    EntityId INT NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    UserId INT NOT NULL,
    UserRole NVARCHAR(50),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    Remarks NVARCHAR(1000),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    INDEX IX_EntityTypeId (EntityType, EntityId),
    INDEX IX_CreatedDate (CreatedDate DESC),
    INDEX IX_UserId (UserId),
    FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
```

### AccountTransaction Table
```sql
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
    INDEX IX_AccountId (AccountId),
    INDEX IX_CreatedDate (CreatedDate DESC),
    INDEX IX_ReferenceId (ReferenceId),
    FOREIGN KEY (AccountId) REFERENCES AccountBalance(SubdealerAccountId),
    FOREIGN KEY (InitiatedBy) REFERENCES [User](UserId)
);
```

---

## Audit UI Requirements

### Audit Trail Screen
**Path:** `/Admin/Audit/Trail`

**Display:**
```
Timestamp | User | Action | Entity | Changes | Remarks
2024-08-07 15:30:45 | Admin User | Approve | PurchaseOrder #1001 | Status: Pending → Approved | Verified
```

**Filters:**
- Date Range (From, To)
- Entity Type (dropdown)
- User (dropdown)
- Action (Create, Update, Delete, Approve, Reject)
- Search by Entity ID or Remarks

**Export Options:**
- CSV Export (with "Exported by User X on Date Y")
- PDF Report

### Transaction History Screen
**Path:** `/Subdealer/Account/Transactions` or `/Admin/Transactions`

**Display:**
```
Date | Type | Amount | Reference | Balance After | Reason | Initiated By
2024-08-07 15:30:45 | Debit | ₹500,000 | PurchaseOrder #2001 | ₹4,500,000 | Order Approved | Admin
```

**Filters:**
- Date Range
- Transaction Type (Debit, Credit)
- Reference Type (PurchaseOrder, Commission, Return, Payment)
- Amount Range

---

## Implementation Strategy

### For Every Create/Update/Delete Operation:

1. **Capture old state** (if update/delete)
2. **Perform operation**
3. **Create AuditLog entry** with:
   - WHO: UserId, User.UserRole
   - WHEN: DateTime.UtcNow
   - WHAT: EntityType, EntityId, OldValue, NewValue (JSON serialized)
   - WHY: Remarks (if provided by user)
   - SOURCE: IpAddress (from HttpContext), UserAgent (from HttpContext)

4. **Create AccountTransaction** (if balance-related operation)

### AuditLog Creation Helper Service:

```csharp
public interface IAuditService
{
    Task LogActionAsync(string entityType, int entityId, string action, 
                       int userId, string userRole, string oldValue, 
                       string newValue, string remarks = null);
    
    Task LogTransactionAsync(int accountId, int transactionType, 
                           decimal amount, decimal balanceAfter, 
                           string reason, int? referenceId, 
                           string referenceType, string remarks, 
                           int initiatedBy);
    
    Task<IEnumerable<AuditLog>> GetAuditTrailAsync(string entityType = null, 
                                                   int? entityId = null, 
                                                   DateTime? fromDate = null, 
                                                   DateTime? toDate = null);
}
```

### Usage in Controllers:

```csharp
// Create Operation
var oldValue = JsonSerializer.Serialize(existingOrder);
var newValue = JsonSerializer.Serialize(updatedOrder);

await _auditService.LogActionAsync(
    entityType: "PurchaseOrder",
    entityId: order.OrderId,
    action: "Approve",
    userId: User.GetUserId(),
    userRole: User.GetRole(),
    oldValue: oldValue,
    newValue: newValue,
    remarks: approvalRemarks
);
```

---

## Compliance & Retention

### Audit Log Retention
- Keep all audit logs permanently (immutable)
- Archive to separate database after 2 years
- Cannot be deleted (only soft-delete via IsActive flag)

### Compliance Reports
- Generate monthly audit summary
- Generate audit trail for compliance audits
- Export audit logs for regulatory reviews

### Data Protection
- Mask sensitive data in audit logs (passwords, card details)
- Encrypt audit logs in transit and at rest
- Restrict audit log access to admin users only

---

## Summary: "Trackability Matrix"

| Screen | WHO | WHEN | WHAT | WHY |
|--------|-----|------|------|-----|
| Create Vehicle Model | UserId, UserRole | CreatedDate | EntityType, EntityId, OldValue, NewValue | Remarks |
| Update Price | UserId, UserRole | CreatedDate | Old/New Price | Price Reason |
| Approve Purchase Order | UserId, UserRole | CreatedDate | OrderId, Status Change | Admin Remarks |
| Submit Commission | UserId, UserRole | CreatedDate | CommissionId, Amount | Subdealer Remarks |
| Approve Payment | UserId, UserRole | CreatedDate | PaymentId, Status | Dealer Remarks |
| Request Return | UserId, UserRole | CreatedDate | ReturnRequestId | Return Reason |

Every single change is fully auditable and traceable.


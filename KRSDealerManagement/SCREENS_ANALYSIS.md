# KRS Dealer Management System - Detailed Screens Analysis

## System Overview

### User Roles
1. **Admin (KRS Owner)** - Full system access, creates dealers/subdealers
2. **Dealer Account Owner** - Access to all dealer + subdealer operations
3. **Dealer Employee (KRS Employee)** - View-only access to subdealer balances
4. **Subdealer** - Purchase vehicles, submit commissions, manage orders

### Business Model
- KRS purchases vehicles at ₹1,00,000 (1 lakh)
- KRS sells to subdealers at ₹1,20,000 (1.2 lakhs) - 20% markup
- Commissions awarded monthly per vehicle (varies by month)
- Commission paid into subdealer account upon admin approval
- Vehicle prices can change monthly (inflation/deflation)
- Subdealers can only purchase within available balance
- Purchase order amounts RESERVED until approved/rejected

---

## ADMIN SCREENS

### Screen 1: Vehicle Model Management
**Path:** `/Admin/VehicleModels`

**Functionality:**
- Add new vehicle models
- Edit existing models
- Mark inactive/active
- Display all models in grid

**Fields:**
- Vehicle Model Name (required)
- Description (optional)

**Database Entities:**
- `VehicleModel` (ModelId, ModelName, Description, IsActive, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)

**Business Rules:**
- Model name must be unique
- Cannot delete (soft delete via IsActive)

---

### Screen 2: Vehicle Color Management
**Path:** `/Admin/VehicleColors`

**Functionality:**
- Select vehicle model
- Add colors for selected model
- Edit color details
- Mark inactive/active

**Fields:**
- Select Model (dropdown)
- Color Name (required)
- Hex Code (optional - for UI display)

**Database Entities:**
- `VehicleColor` (ColorId, ColorName, HexCode, IsActive, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)

**Business Rules:**
- Color name must be unique
- Cannot delete (soft delete via IsActive)

---

### Screen 3: Vehicle Price Management
**Path:** `/Admin/VehiclePrices`

**Functionality:**
- Select vehicle model and color
- Enter price for specific month/year
- Can add multiple prices for same vehicle in single month
- View price history

**Fields:**
- Select Vehicle Model (dropdown)
- Select Vehicle Color (dropdown, filtered by model)
- Enter Price (decimal, required)
- Select Month (1-12, required)
- Select Year (required)
- Notes (optional)

**Database Entities:**
- `VehiclePriceHistory` (PriceHistoryId, VehicleId, Month, Year, Price, Notes, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)

**Business Rules:**
- Multiple prices per vehicle per month allowed
- Latest price in month is active price
- If current month has no price, use previous month's price
- Price history tracks all changes

---

### Screen 4: Create Subdealer
**Path:** `/Admin/Subdealers/Create`

**Functionality:**
- Register new subdealer user
- Enter business details
- Auto-generate initial account

**Fields:**
- Subdealer Name (required)
- Subdealer Location (required)
- Primary Phone (required)
- Secondary Phone (optional)
- Sales Rep Mobile (optional)
- Service Rep Mobile (optional)

**Database Entities:**
- `User` (UserId, Username=auto-generated, Email, PasswordHash, FirstName=Name, UserRole=Subdealer)
- `SubdealerAccount` (AccountId, SubdealerId, AccountName="Main", AccountType="Sales")
- `AccountBalance` (BalanceId, SubdealerAccountId, CurrentBalance=0, ReservedAmount=0)

**Business Rules:**
- Username auto-generated (e.g., "subdealer_001")
- Password auto-generated and shown to admin
- Initial balance = 0 (set by admin later)

---

### Screen 5: Create Subdealer Account
**Path:** `/Admin/SubdealerAccounts/Create`

**Functionality:**
- Create additional accounts for existing subdealer
- Configure menu permissions for account
- Set initial balance

**Fields:**
- Select Dealer/Subdealer (dropdown)
- Select Location (dropdown, filtered by subdealer)
- Username (required)
- Password (required)
- Account Name (e.g., "Branch 1", "Fleet Operations")
- Account Type (e.g., "Sales", "Fleet", "Corporate")

**Database Entities:**
- `SubdealerAccount` (AccountId, SubdealerId, AccountName, AccountType, IsActive, CreatedDate, ModifiedDate)
- `AccountPermission` (PermissionId, AccountId, MenuKey, MenuName, IsAccessible, CanCreate, CanEdit, CanDelete, CanApprove)
- `AccountBalance` (BalanceId, SubdealerAccountId, InitialBalance, CurrentBalance, ReservedAmount, AvailableBalance)

**Business Rules:**
- Each subdealer can have multiple accounts
- Accounts have independent balances
- Menu permissions are account-specific (not user-specific)
- Default permissions: all menus accessible for new account

---

### Screen 6: Commission Amount Management
**Path:** `/Admin/Commissions/Rates`

**Functionality:**
- Set monthly commission amounts per vehicle model
- Commission can vary by month
- Commission effective for period (start month to expiry month)

**Fields:**
- Select Vehicle Model (dropdown)
- Commission Amount (decimal, required)
- Start Month (1-12, required)
- Start Year (required)
- Expiry Month (1-12, required)
- Expiry Year (required)
- Notes (optional)

**Database Entities:**
- Need to add: `CommissionRate` table (CommissionRateId, ModelId, CommissionAmount, StartMonth, StartYear, ExpiryMonth, ExpiryYear, Notes)

**Business Rules:**
- Commission rates are model-based (not color-based)
- One commission amount per month per model
- If no rate for current month, use previous month's rate
- Commission submission by subdealer uses current/previous month rate

---

### Screen 7: Dealer Account Management
**Path:** `/Admin/DealerAccounts`

**Functionality:**
- Manage main dealer (KRS) account
- Create dealer accounts for staff (Owner, Dealer Admin, KRS Employee)
- Configure permissions per role

**Account Types:**
1. **Owner** - Full access to everything (dealer + subdealers + admin)
2. **Owner Admin** - Can create vehicle models, colors, prices (subset of owner)
3. **KRS Employee** - View-only: can see subdealer closing/available balances only

**Fields:**
- Account Type (dropdown: Owner, Owner Admin, KRS Employee)
- Username (required)
- Password (required)
- Full Name (required)

**Database Entities:**
- `User` (UserId, Username, Email, PasswordHash, FirstName, UserRole=Admin, IsActive)
- `SubdealerAccount` (special account for dealer staff with role-based permissions)
- `AccountPermission` (role-specific menu access)

**Business Rules:**
- Owner has all permissions
- Owner Admin limited to: VehicleModels (Create, Edit), VehicleColors (Create, Edit), VehiclePrices (Create, Edit)
- KRS Employee limited to: View AccountBalance (read-only)

---

## SUBDEALER SCREENS

### Screen 1: Create Purchase Order
**Path:** `/Subdealer/Orders/Create`

**Functionality:**
- Create purchase order for vehicles
- Select multiple vehicle models/colors
- Add multiple items until balance runs out
- System prevents exceeding available balance
- Amount gets RESERVED (locked) until admin approves/rejects

**Fields:**
- Select Model (dropdown)
- Select Color (dropdown, filtered by model)
- Add to Order (button adds row)
- Grid showing:
  - Model | Color | Current Month Price | Qty | Total Amount
  - X (remove button)
- Total Order Amount (calculated)
- Notes (optional)
- Submit Button (creates order)

**Database Entities:**
- `PurchaseOrder` (OrderId, AccountId, SubdealerId, OrderNumber, TotalQuantity, TotalAmount, Status=Pending, CreatedDate)
- `Vehicle` (VehicleId, ModelId, ColorId, ChassisNumber, Status=Available/Reserved, CreatedDate)
- `AccountBalance` (updated: ReservedAmount increased, AvailableBalance decreased)

**Business Rules:**
- Cannot add item if total > available balance
- When order submitted: ReservedAmount += OrderTotal
- AvailableBalance = CurrentBalance - ReservedAmount
- All items in order tied to same PurchaseOrder record
- Each vehicle gets unique ChassisNumber (to be generated or entered)
- Order status: Pending → (Approved/Rejected)

**Flow:**
1. Subdealer selects model/color → system shows current price
2. Subdealer enters quantity → system shows item total
3. Subdealer clicks "Add to Order" → item added to grid
4. Subdealer repeats until satisfied or balance runs out
5. Subdealer clicks "Submit Order" → creates PurchaseOrder with Pending status
6. System reserves amount from account balance
7. Order sent to dealer/admin for approval

---

### Screen 2: Commission Submission
**Path:** `/Subdealer/Commissions/Submit`

**Functionality:**
- Submit commission for specific vehicle in specific month
- System auto-fills commission amount based on rates
- One commission per vehicle per month

**Fields:**
- Select Model (dropdown)
- Select Color (dropdown, filtered by model)
- Enter Chassis Number (required - to identify specific vehicle)
- Select Month (dropdown 1-12, required)
- Commission Amount (auto-filled from rates, editable)
- Remarks (optional)
- Submit Button

**Database Entities:**
- `Commission` (CommissionId, AccountId, SubdealerId, VehicleId, Month, Year, CommissionAmount, Status=Pending, CreatedDate)

**Business Rules:**
- Commission amount auto-filled from `CommissionRate` table
- If no rate for selected month, show previous month's rate
- One commission per vehicle per month (prevent duplicates)
- Status: Pending → (Approved/Paid or Rejected)
- Upon admin approval: Status = Approved, then Paid
- When paid: amount added to account balance as credit

---

### Screen 3: Account Details / Dashboard
**Path:** `/Subdealer/Account/Details`

**Functionality:**
- Display account information
- Show current balance, reserved amount, available balance
- Show transaction history
- Show recent orders and commissions

**Display:**
- Account Name
- Current Balance: ₹X,XX,XXX
- Reserved Amount: ₹X,XX,XXX
- Available Balance: ₹X,XX,XXX
- Last Transaction Date: DD/MM/YYYY

**Sections:**
1. **Recent Orders** (grid: OrderNumber | Qty | Amount | Status | Date)
2. **Recent Commissions** (grid: Model | Month | Amount | Status | Date)
3. **Transaction History** (grid: Date | Type | Amount | Balance | Remarks)

**Database Entities:**
- `AccountBalance`
- `PurchaseOrder`
- `Commission`
- `AccountTransaction` (if implementing detailed transaction log)

---

### Screen 4: Purchase Order History
**Path:** `/Subdealer/Orders/History`

**Functionality:**
- View all purchase orders
- View items within each order
- Option to request return on specific vehicle
- Track order status changes

**Grid:**
- OrderNumber | Date | Total Qty | Total Amount | Status | Actions

**Order Details Modal/Page:**
- Order details with items table:
  - Model | Color | Qty | Unit Price | Total | Status
- For each approved vehicle: "Return Request" button
- For pending order: "Cancel" button
- Remarks section (system notes on approval/rejection)

**Return Request Flow:**
- Click "Return Request" on approved vehicle
- System creates return entry
- Amount held pending admin approval
- Upon admin approval: amount refunded to account balance

**Database Entities:**
- `PurchaseOrder`
- `Vehicle`
- Need to add: `ReturnRequest` table (ReturnRequestId, VehicleId, OrderId, Status, ApprovedBy, ApprovedDate)

---

### Screen 5: Payment Management
**Path:** `/Subdealer/Payments`

**Functionality:**
- Record payments made to dealer
- View payment history
- Payment tracking for reconciliation

**Add Payment:**
- Amount (required, decimal)
- Payment Type (dropdown: Cash, GPay, NEFT, Others)
- If Others: text field for payment method
- Payment Date (required, date picker)
- Remarks (optional)

**Payment History Grid:**
- Date | Amount | Type | Status | Remarks

**Database Entities:**
- Need to add: `Payment` table (PaymentId, AccountId, Amount, PaymentType, PaymentDate, Status, Remarks, CreatedDate)

**Business Rules:**
- Payments are voluntary (subdealer initiative)
- Default status: Pending (awaiting dealer approval)
- Dealer can approve/reject with remarks
- Upon approval: amount can be applied to balance (at dealer discretion)

---

## DEALER SCREENS

### Screen 1: Manage Purchase Orders (Approval)
**Path:** `/Dealer/Orders/Manage`

**Functionality:**
- View all pending purchase orders from subdealers
- Approve/reject individual vehicles in order
- Individual vehicle approval/rejection with remarks

**Grid:**
- Subdealer Name | OrderNumber | Date | Total Qty | Status | Actions

**Order Details:**
- Show order header with order info
- Table of vehicles in order:
  - Model | Color | Unit Price | Status | Qty | Total Amount | [Approve] [Reject] buttons
- Remarks textarea for each vehicle action
- Comments box for order-level remarks

**Approval Logic:**
- When [Approve] clicked on vehicle:
  - Status changes to Approved
  - Amount deducted from subdealer's CurrentBalance
  - ReservedAmount decreased by that vehicle amount
  - Remarks recorded with timestamp and approver info
  
- When [Reject] clicked on vehicle:
  - Status changes to Rejected
  - Amount released back to available balance
  - ReservedAmount decreased by that amount
  - Remarks recorded

**Database Entities:**
- `PurchaseOrder`
- `Vehicle`
- `AccountBalance` (updated on each approval/rejection)
- `AuditLog` (tracks who approved/rejected, when, with remarks)

---

### Screen 2: Create Order for Subdealer
**Path:** `/Dealer/Orders/Create`

**Functionality:**
- Dealer allocates vehicles directly to subdealer
- No approval needed (dealer is approver)
- Amount automatically deducted from subdealer account
- Used for dealer-initiated stock distribution

**Fields:**
- Select Subdealer Account (dropdown)
- Select Model (dropdown)
- Select Color (dropdown, filtered by model)
- Enter Quantity (required)
- Add to Order (button)
- Grid showing order items
- Submit Button

**Business Rules:**
- Similar to subdealer order creation
- But: No approval stage (dealer creates and auto-approves)
- Amount immediately deducted from subdealer balance
- No reservation needed (direct debit)

**Database Entities:**
- `PurchaseOrder` (Status=Approved, ApprovedBy=DealerUserId, ApprovedDate=now)
- `Vehicle`
- `AccountBalance` (updated immediately)
- `AccountTransaction` (record debit transaction)

---

### Screen 3: Return Request Approval
**Path:** `/Dealer/Returns/Manage`

**Functionality:**
- View all return requests from subdealers
- Approve/reject returns
- Upon approval: refund amount to subdealer

**Grid:**
- Subdealer Name | OrderNumber | Vehicle (Model/Color) | Amount | Date Requested | Status | Actions

**Return Details:**
- Return reason (if provided)
- Vehicle details (Model, Color, ChassisNumber)
- Original purchase amount
- [Approve] [Reject] buttons
- Remarks textarea

**Approval Logic:**
- When [Approve]:
  - Status = Approved
  - Amount added back to subdealer's CurrentBalance
  - Remarks recorded with timestamp
  
- When [Reject]:
  - Status = Rejected
  - Remarks recorded

**Database Entities:**
- `ReturnRequest`
- `AccountBalance` (updated on approval)
- `AccountTransaction` (credit transaction)

---

### Screen 4: Payment Approval
**Path:** `/Dealer/Payments/Manage`

**Functionality:**
- View all payments submitted by subdealers
- Approve/reject payments
- Add remarks on approval/rejection

**Grid:**
- Subdealer Name | Date | Amount | Type | Status | Actions

**Payment Details:**
- Payment method details
- Amount
- Subdealer remarks
- [Approve] [Reject] buttons
- Dealer remarks textarea

**Approval Logic:**
- When [Approve]:
  - Status = Approved
  - Dealer can optionally apply to balance (at their discretion)
  
- When [Reject]:
  - Status = Rejected
  - Reason recorded

**Database Entities:**
- `Payment`
- `AccountTransaction` (optional, if applied)

---

## CROSS-CUTTING CONCERNS

### Audit Trail Requirements
All actions (Create, Approve, Reject, etc.) must record:
- Who performed action (UserId)
- When (Timestamp UTC)
- What action (Create/Approve/Reject/Return/etc)
- Remarks (admin/dealer comments)

**Database Entity:**
- `AuditLog` (AuditLogId, EntityType, EntityId, Action, UserId, Timestamp, Remarks, OldValue, NewValue)

### Transaction Tracking
All balance changes must be recorded:
- Debit (order approval, payment)
- Credit (commission approval, return approval)
- Amount
- Reference (OrderId, CommissionId, etc.)
- Timestamp

**Database Entity:**
- `AccountTransaction` (TransactionId, AccountId, Type, Amount, ReferenceId, Remarks, CreatedDate)

### Balance Reserved vs Available
- **CurrentBalance:** Actual money in account
- **ReservedAmount:** Money locked for pending orders
- **AvailableBalance:** CurrentBalance - ReservedAmount (what can be used)

When purchase order created: ReservedAmount += OrderAmount
When purchase order approved: CurrentBalance -= OrderAmount, ReservedAmount -= OrderAmount
When purchase order rejected: ReservedAmount -= OrderAmount (balance freed)

---

## KEY BUSINESS FLOWS

### Flow 1: Subdealer Purchase Order
```
1. Subdealer creates purchase order (reserves amount)
2. Dealer reviews and approves/rejects individual vehicles
3. On approval: amount deducted from account, vehicle status = Sold
4. On rejection: amount returned to available balance
5. Subdealer can later request return (if approved)
6. On return approval: amount refunded
```

### Flow 2: Commission Payment
```
1. Subdealer submits commission for vehicle in month
2. System auto-fills amount from rates (or previous month)
3. Admin approves commission
4. System credits amount to subdealer account
5. Commission status = Paid
```

### Flow 3: Account Balance Updates
```
Initial Balance: ₹10,00,000
- Order 1 (5 vehicles @ ₹1,00,000 each = ₹5,00,000) → Reserved
  - Available: ₹5,00,000 | Current: ₹10,00,000 | Reserved: ₹5,00,000

- Admin approves 4 vehicles, rejects 1 vehicle
  - Current: ₹9,00,000 (4 @ 1,00,000 deducted)
  - Reserved: ₹1,00,000 (1 pending rejection)
  - Available: ₹8,00,000

- Rejection processed, amount released
  - Current: ₹9,00,000
  - Reserved: ₹0
  - Available: ₹9,00,000

- Commission approved (₹25,000)
  - Current: ₹9,25,000
  - Available: ₹9,25,000

- Return request approved (₹1,00,000 refund)
  - Current: ₹10,25,000
  - Available: ₹10,25,000
```

---

## PERMISSION MATRIX

| Feature | Owner | Owner Admin | KRS Employee | Subdealer |
|---------|-------|------------|--------------|-----------|
| Vehicle Models | C,R,U,D | C,R,U,D | R | - |
| Vehicle Colors | C,R,U,D | C,R,U,D | R | - |
| Vehicle Prices | C,R,U,D | C,R,U,D | R | - |
| Commission Rates | C,R,U,D | - | R | - |
| View Subdealer Balance | R | - | R | R (own) |
| Approve Purchase Order | R,U | - | - | - |
| Create Purchase Order | - | - | - | C |
| Submit Commission | - | - | - | C |
| Return Requests | R,U | - | - | - |
| Payment Approval | R,U | - | - | - |

---

## NEXT STEPS FOR APPLICATION LAYER

Based on this analysis, the Application Layer needs:

**DTOs:**
- VehicleModelDto, VehicleColorDto, VehiclePriceDto
- SubdealerDto, SubdealerAccountDto
- PurchaseOrderDto, CommissionDto, ReturnRequestDto, PaymentDto
- AccountBalanceDto, AccountDetailsDto

**Commands:**
- CreateVehicleModelCommand, UpdateVehicleModelCommand
- CreateVehiclePriceCommand
- CreateSubdealerCommand
- CreatePurchaseOrderCommand, ApprovePurchaseOrderItemCommand, RejectPurchaseOrderItemCommand
- SubmitCommissionCommand, ApproveCommissionCommand
- CreateReturnRequestCommand, ApproveReturnRequestCommand
- CreatePaymentCommand, ApprovePaymentCommand

**Queries:**
- GetVehicleModelsQuery, GetVehicleColorsQuery
- GetPurchaseOrdersQuery (filtered by status, subdealer)
- GetAccountBalanceQuery
- GetCommissionsQuery (filtered by status)
- GetReturnRequestsQuery, GetPaymentsQuery


# Dealer-Subdealers Vehicle Management System - Requirements Specification

**Project Name:** Vehicle Dealer Management System (VDMS)  
**Technology Stack:** ASP.NET Core (Razor Pages) + SQL Server  
**Date Created:** August 7, 2026  
**Version:** 1.0

---

## 1. Executive Summary

This is a web-based vehicle management and commission tracking system for a dealer with 28 subdealers. The system manages vehicle inventory, pricing, purchase orders, and monthly commission payments with complete audit trail capabilities.

---

## 2. System Users & Roles

### 2.1 Admin (Dealer)
- Primary administrator of the system
- Manages vehicle catalog (models, colors)
- Updates vehicle prices monthly
- Approves/rejects subdealers' purchase requests
- Approves subdealers' monthly commission claims
- Can create purchase orders in minus (negative) amount

### 2.2 Subdealers (28 users)
- Purchase vehicles from dealer
- Track their account balance and transactions
- Submit monthly commission claims
- View vehicle inventory and pricing
- Cannot purchase in minus (negative) amount
- Can only purchase up to their current balance

---

## 3. Core Business Rules

### 3.1 Vehicle Pricing
- Admin can set/update vehicle prices monthly
- Price changes apply only to **new** purchase orders
- For existing vehicles (Purchased status): Price can increase/decrease before invoicing
- If price increases: Additional amount automatically debited from subdealers' account
- If price decreases: Difference credited back to subdealers' account
- **Audit Requirement:** Track all price changes with dates and reasons

### 3.2 Purchase Order Workflow
1. **Subdealers creates purchase request** with quantity and vehicle selection
2. System validates: Account balance >= Total order value
3. **Amount is reserved** (blocked) from subdealers' account
4. **Admin approves/rejects** the purchase request
5. **On Approval:** 
   - Amount is debited from account
   - Vehicles created with "Purchased" status
   - Chassis numbers assigned
6. **On Rejection:** 
   - Reserved amount is released back to account
   - Subdealers can place new order

### 3.3 Vehicle Lifecycle & Status

Each vehicle has a unique **Chassis Number** with the following status flow:

1. **Purchased** - Vehicle bought from dealer (awaiting invoice)
   - Price can still change during this phase
   - Amount can be adjusted if price changes
   
2. **Invoiced** - Invoice generated, price locked
   - No further price changes allowed
   - Commission becomes eligible for next month

3. **RTO Initiated** - Registration initiated with RTO (government)
   
4. **RTO Number Given** - Vehicle fully registered

### 3.4 Commission Management
- **Trigger:** After a vehicle reaches "Invoiced" status at month-end
- **For Each Vehicle:** Subdealers can submit monthly commission claim
- **Commission Amount:** Can vary each month (admin sets or subdealers proposes)
- **Approval:** Admin reviews and approves/rejects each vehicle's commission
- **On Approval:** Commission amount added to subdealers' account balance
- **Audit Trail:** Track commission submission, approval date, and amount

### 3.5 Account Balance Management

**For Subdealers:**
- Starting balance: Set by admin
- Cannot go into minus (all transactions must have sufficient balance)
- Reserved amount: Blocked when purchase order is created
- Debit: When purchase order approved, price adjustment increases, or commission approved
- Credit: When purchase order rejected, price adjustment decreases, or commission approved

**For Dealer (Admin):**
- Can create purchase requests with minus balance
- No restrictions on account balance

### 3.6 Audit & Tracking Requirements

All changes must be tracked with:
- **Who:** User ID (admin/subdealers)
- **What:** Change details (vehicle price, commission, purchase status)
- **When:** Timestamp (date/time)
- **Why:** Change reason/notes

**Fallback Logic for Missing Data:**
- If price not set for current month: Use previous month's price
- If commission not set for current month: Use previous month's commission
- Maintain complete history for audit purposes

---

## 4. Functional Requirements

### 4.1 Login & Authentication
- [ ] Login page with username and password
- [ ] Role-based access (Admin vs Subdealers)
- [ ] Session management
- [ ] Secure password storage (hash/salt)

### 4.2 Admin Screens

#### 4.2.1 Dashboard
- [ ] Overview of vehicle inventory
- [ ] Pending purchase orders count
- [ ] Pending commission approvals count

#### 4.2.2 Vehicle Management
- [ ] Add new vehicle model
- [ ] Add vehicle color variants
- [ ] View all vehicles
- [ ] Edit vehicle details

#### 4.2.3 Price Management
- [ ] Set vehicle price per month
- [ ] View price history with dates
- [ ] Compare previous month vs current month price
- [ ] Bulk price updates

#### 4.2.4 Purchase Order Management
- [ ] View all purchase orders with filters (status, subdealers, date)
- [ ] Approve purchase order (creates vehicles, debits account)
- [ ] Reject purchase order (releases reserved amount)
- [ ] View order details (vehicle details, quantity, total amount)

#### 4.2.5 Commission Approval
- [ ] View pending commission requests
- [ ] Filter by subdealers, vehicle, month
- [ ] Approve commission (adds to account)
- [ ] Reject commission (no change to account)
- [ ] View commission history

#### 4.2.6 Subdealers Management
- [ ] View all subdealers
- [ ] View subdealers' account balance
- [ ] Set initial account balance
- [ ] View transaction history

### 4.3 Subdealers Screens

#### 4.3.1 Dashboard
- [ ] Quick summary: Current balance, pending orders, approved commissions

#### 4.3.2 Purchase Vehicles
- [ ] Browse available vehicles (model, color, price)
- [ ] Select quantity and vehicle
- [ ] View total cost vs available balance
- [ ] Submit purchase request
- [ ] View purchase history and status

#### 4.3.3 Commission Entry
- [ ] View invoiced vehicles available for commission
- [ ] Enter monthly commission per vehicle
- [ ] Submit commission claim
- [ ] View commission history and approval status

#### 4.3.4 Account Screen (Complete Audit Trail)
- [ ] Current account balance
- [ ] Transaction history (date, type, amount, balance after transaction)
  - Purchase order approval (debit)
  - Purchase order rejection (credit)
  - Price adjustment (debit/credit)
  - Commission approval (credit)
- [ ] Filter by date range, transaction type
- [ ] Export account statement

---

## 5. Data Models Overview

### 5.1 Core Entities
1. **User** - Admin & Subdealers
2. **Vehicle Model** - Car model (BMW, Toyota, etc.)
3. **Vehicle Color** - Color variants
4. **Vehicle** - Individual vehicles with chassis numbers
5. **Vehicle Price History** - Monthly pricing with audit trail
6. **Commission History** - Monthly commission with approval status
7. **Purchase Order** - Subdealers' vehicle purchase requests
8. **Account Transaction** - All account movements with audit trail
9. **Account Balance** - Current balance per subdealers

### 5.2 Status Enums
- User Role: Admin, Subdealers
- Vehicle Status: Purchased, Invoiced, RTO Initiated, RTO Number Given
- Purchase Order Status: Pending, Approved, Rejected
- Commission Status: Pending, Approved, Rejected
- Transaction Type: Purchase Approved, Purchase Rejected, Price Adjustment, Commission Approved, Commission Rejected, Initial Balance

---

## 6. Data Flow Scenarios

### Scenario 1: Purchase Order Approval with Price Change
1. Subdealers has balance: 1,00,000 INR
2. Subdealers purchases 10 vehicles at 1,00,000 INR each (1,000,000 INR total)
3. Admin approves for 9 vehicles (900,000 INR)
4. Balance after approval: 1,00,000 - 900,000 = -8,00,000 (Reserved: 1,00,000 for 1 rejected)
5. Price increases to 1,01,000 for the 9 approved vehicles
6. Additional 9,000 INR debited from account
7. Final balance: -8,00,000 - 9,000 = -8,09,000

### Scenario 2: Monthly Commission Flow
1. Vehicle reaches "Invoiced" status in April
2. April month ends
3. Subdealers submits commission claim: 4,000 INR for that vehicle
4. Admin approves: 4,000 INR added to account balance
5. May: If no new commission submitted, system uses April's 4,000 INR (fallback logic)
6. If May commission is submitted: Use May's amount instead

### Scenario 3: Price Decrease Scenario
1. Vehicle purchased at 1,00,000 INR (reserved from account)
2. Before invoicing, price decreases to 99,000 INR
3. Difference (1,000 INR) credited back to account automatically

---

## 7. Technical Requirements

### 7.1 Architecture
- **Layered Architecture:**
  - Presentation Layer: Razor Pages
  - Business Logic Layer: Services
  - Data Access Layer: Entity Framework Core
  - Database Layer: SQL Server

### 7.2 Database
- SQL Server
- Entity Framework Core (Code-First approach)
- Migrations for version control
- Audit tables for all changes

### 7.3 Security
- Password hashing (bcrypt or similar)
- Authorization checks on all pages
- CSRF protection
- Input validation and sanitization
- SQL injection prevention (EF Core parameterized queries)

### 7.4 Error Handling
- User-friendly error messages
- Logging of all errors
- Validation feedback on forms

---

## 8. Success Criteria

- [ ] All users can login with role-based access
- [ ] Admin can manage vehicles, prices, and approve requests
- [ ] Subdealers can view balance and purchase vehicles within limits
- [ ] Price changes automatically debit/credit subdealers accounts
- [ ] Commission workflow functions end-to-end
- [ ] All transactions are audited with timestamp and user
- [ ] Fallback logic works for missing monthly data
- [ ] System prevents negative balance for subdealers
- [ ] System maintains complete audit trail for compliance

---

## 9. Future Enhancements (Out of Scope)

- Reporting and analytics dashboard
- Email notifications for approvals
- Multi-currency support
- Mobile app
- API for third-party integrations
- Advanced search and filtering
- Dashboard analytics

---

## 10. Glossary

- **Dealer:** Primary admin user
- **Subdealers:** Vehicle purchasers (28 users)
- **Chassis Number:** Unique vehicle identifier
- **Invoicing:** Process of locking price and making commission eligible
- **RTO:** Road Transport Office (government registration)
- **Reserve Amount:** Money blocked in account for pending purchase orders
- **Commission:** Monthly incentive paid to subdealers per vehicle
- **Audit Trail:** Complete history of all changes with timestamps

---

**Document Prepared By:** Kiro AI  
**Last Updated:** August 7, 2026

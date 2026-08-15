# KRS EV Dealer Management - Setup Checklist

## ✅ Pre-Execution Checklist

Before running the database script, verify:

- [ ] SQL Server Express installed on machine
- [ ] SQL Server Management Studio (SSMS) installed
- [ ] SQL Server service running (Services → MSSQL$SQLEXPRESS = Started)
- [ ] SSMS can connect to localhost\SQLEXPRESS
- [ ] File exists: `d:\KRS\Specifications\DATABASE_INIT.sql`
- [ ] File size is reasonable (> 20KB)

---

## 🚀 Execution Steps

### Step 1: Launch SSMS
- [ ] Open SQL Server Management Studio
- [ ] Server name: `localhost\SQLEXPRESS`
- [ ] Authentication: Windows
- [ ] Click Connect
- [ ] Verify Object Explorer shows databases

### Step 2: Open Script
- [ ] Click File → Open → File...
- [ ] Navigate to: `d:\KRS\Specifications\`
- [ ] Select: `DATABASE_INIT.sql`
- [ ] Click Open
- [ ] Verify script content displays in editor

### Step 3: Execute Script
- [ ] Press F5 (or click Execute button)
- [ ] Do NOT close SSMS
- [ ] Wait for execution to complete (30-60 seconds)
- [ ] Check Messages tab for "Seed Data Inserted Successfully"

---

## ✅ Post-Execution Verification

### Step 1: Verify Database Created
```sql
-- Run this query in SSMS
SELECT name FROM sys.databases WHERE name = 'VehicleDealerDB';
```
- [ ] Result shows: VehicleDealerDB

### Step 2: Verify Tables Created
```sql
-- In Object Explorer
-- Expand: Databases → VehicleDealerDB → Tables
```
- [ ] Should see 10 tables:
  - [ ] Users
  - [ ] VehicleModels
  - [ ] VehicleColors
  - [ ] VehiclePriceHistory
  - [ ] PurchaseOrders
  - [ ] Vehicles
  - [ ] CommissionHistory
  - [ ] AccountBalance
  - [ ] AccountTransactions
  - [ ] AuditLog

### Step 3: Verify Seed Data
```sql
-- Check user count
SELECT COUNT(*) FROM [Users];
```
- [ ] Result: 29 (1 admin + 28 subdealers)

```sql
-- Check total balance
SELECT SUM(CurrentBalance) FROM [AccountBalance];
```
- [ ] Result: 28000000 (₹2.8 crores)

```sql
-- Check EV models
SELECT COUNT(*) FROM [VehicleModels];
```
- [ ] Result: 8

```sql
-- Check pricing records
SELECT COUNT(*) FROM [VehiclePriceHistory];
```
- [ ] Result: 48 (8 models × 6 colors)

---

## 🔐 Verify User Accounts

### Admin Account
```sql
SELECT * FROM [Users] WHERE Username = 'admin';
```
- [ ] Username: admin
- [ ] Email: admin@krsdealers.com
- [ ] UserRole: 1 (Admin)

### Sample Subdealer
```sql
SELECT * FROM [Users] WHERE Username = 'subdealer_001';
```
- [ ] Username: subdealer_001
- [ ] Email: subdealer001@krsdealers.com
- [ ] UserRole: 2 (Subdealer)

---

## 🚗 Verify EV Models

```sql
SELECT ModelName, (SELECT Price FROM VehiclePriceHistory WHERE ModelId = VehicleModels.ModelId LIMIT 1) as Price
FROM [VehicleModels] ORDER BY ModelId;
```
- [ ] 8 rows displayed
- [ ] Names include: Tesla, Tata, Hyundai, MG, Mahindra, Citroen, Audi

---

## 💰 Verify Balance Setup

```sql
SELECT sa.AccountId, u.Username, ab.CurrentBalance, ab.ReservedAmount, ab.AvailableBalance
FROM [AccountBalance] ab
JOIN [Users] u ON ab.SubdealerId = u.UserId
ORDER BY ab.AccountId;
```
- [ ] 28 rows displayed
- [ ] Each has CurrentBalance = 1000000 (₹10,00,000)
- [ ] ReservedAmount = 0 (no reservations yet)
- [ ] AvailableBalance = 1000000

---

## 📝 Application Setup Checklist

After database is ready:

### Update Connection String
- [ ] Open `appsettings.json`
- [ ] Set ConnectionString:
  ```json
  "Server=localhost\\SQLEXPRESS;Database=VehicleDealerDB;Trusted_Connection=true;Encrypt=false;"
  ```

### Build Application
- [ ] Open solution in Visual Studio
- [ ] Right-click Solution → Build Solution
- [ ] Verify build succeeds (no errors)

### Test Database Connection
- [ ] Create test program:
  ```csharp
  var connString = "Server=localhost\\SQLEXPRESS;Database=VehicleDealerDB;Trusted_Connection=true;Encrypt=false;";
  using (var connection = new SqlConnection(connString))
  {
      connection.Open();
      Console.WriteLine("Connected successfully!");
  }
  ```
- [ ] Run test
- [ ] Verify output: "Connected successfully!"

### Test User Login
- [ ] Start application
- [ ] Login page displays
- [ ] Login as admin:
  - [ ] Username: admin
  - [ ] Email: admin@krsdealers.com
- [ ] Verify successful login

### Test Subdealer Access
- [ ] Login as subdealer_001:
  - [ ] Username: subdealer_001
  - [ ] Email: subdealer001@krsdealers.com
- [ ] Verify account dashboard shows ₹10,00,000 balance

---

## 🧪 Test Core Workflows

### Test 1: View EV Models
- [ ] Admin dashboard shows 8 EV models
- [ ] Models include Tesla, Tata, Hyundai, etc.
- [ ] Prices are visible (₹12L to ₹85L range)

### Test 2: Create Purchase Order (Subdealer)
- [ ] Subdealer creates new order
- [ ] Can select model and color
- [ ] Total amount calculated
- [ ] Order status shows "Pending"
- [ ] Balance shows reserved amount

### Test 3: Approve Order (Admin)
- [ ] Admin sees pending orders
- [ ] Admin approves order
- [ ] Status changes to "Approved"
- [ ] Subdealer balance debited
- [ ] Audit log records transaction

### Test 4: View Audit Log
- [ ] Verify audit logs show:
  - [ ] WHO (User ID)
  - [ ] WHAT (Action: Create/Approve)
  - [ ] WHEN (Timestamp)
  - [ ] WHY (Remarks)

### Test 5: View Transaction History
- [ ] Account transactions show:
  - [ ] Date of transaction
  - [ ] Type (Debit/Credit/Reserved)
  - [ ] Amount
  - [ ] Balance after transaction

---

## 🔍 Troubleshooting Checklist

If something fails:

### Database Connection Failed
- [ ] Check SQL Server service running
- [ ] Check SSMS can connect (Object Explorer)
- [ ] Check Windows credentials have access
- [ ] Try running SSMS as Administrator

### Script Execution Failed
- [ ] Check file path is correct
- [ ] Check file is readable
- [ ] Try opening in Notepad to verify content
- [ ] Run with "Results to Text" for error details
- [ ] Check for special characters in file

### Users Not Found
- [ ] Run: SELECT COUNT(*) FROM [Users];
- [ ] If 0, manually insert:
  ```sql
  INSERT INTO [Users] (Username, Email, PasswordHash, FirstName, LastName, UserRole, PhoneNumber, IsActive)
  VALUES ('admin', 'admin@krsdealers.com', 'hash', 'Admin', 'User', 1, '9876543210', 1);
  ```

### Balance Not Showing
- [ ] Check AccountBalance table exists
- [ ] Run: SELECT * FROM [AccountBalance];
- [ ] If empty, insert balances:
  ```sql
  INSERT INTO [AccountBalance] (SubdealerId, CurrentBalance, ReservedAmount, AvailableBalance, InitialBalance, CreatedDate)
  SELECT UserId, 1000000.00, 0, 1000000.00, 1000000.00, GETUTCDATE()
  FROM [Users] WHERE UserRole = 2;
  ```

### Models Not Showing
- [ ] Check VehicleModels table
- [ ] Run: SELECT * FROM [VehicleModels];
- [ ] If empty, insert models manually

---

## 📊 Final Status Verification

Before considering setup complete:

- [ ] ✅ Database: VehicleDealerDB exists
- [ ] ✅ Tables: 10 tables created with data
- [ ] ✅ Users: 29 (1 admin + 28 subdealers)
- [ ] ✅ EV Models: 8 with pricing
- [ ] ✅ Balances: 28 accounts with ₹10L each
- [ ] ✅ Audit: AuditLog table exists
- [ ] ✅ Connection: Application connects successfully
- [ ] ✅ Login: Admin login works
- [ ] ✅ Dashboard: Shows correct data
- [ ] ✅ Workflows: Core operations work

---

## 📁 File Locations Reference

| File | Location |
|------|----------|
| DATABASE_INIT.sql (USE THIS) | `d:\KRS\Specifications\DATABASE_INIT.sql` |
| Setup Instructions | `d:\KRS\Requirement\DATABASE_SETUP_FIX.md` |
| Quick Start | `d:\KRS\Requirement\RUN_THIS_NOW.txt` |
| Error Resolution | `d:\KRS\DATABASE_ERROR_RESOLUTION.md` |
| Setup Checklist | `d:\KRS\SETUP_CHECKLIST.md` (this file) |

---

## ✨ Setup Status

**Current Status:** 
- [ ] Not Started
- [ ] In Progress
- [ ] Completed (all items checked)

**Date Started:** __________
**Date Completed:** __________
**Total Time:** __________

---

## 🎯 Next Actions

After completing this checklist:

1. [ ] Proceed to UI development (Controllers/Views)
2. [ ] Set up authentication/authorization
3. [ ] Create admin dashboard
4. [ ] Create subdealer dashboard
5. [ ] Implement purchase order workflow
6. [ ] Implement commission workflow
7. [ ] Test end-to-end workflows
8. [ ] Deploy to production

---

## 📞 Support

If you get stuck:

1. Check this checklist for your specific error
2. Review `DATABASE_ERROR_RESOLUTION.md`
3. Check `DATABASE_SETUP_FIX.md` for detailed instructions
4. Verify file paths and permissions
5. Ensure SQL Server service is running

---

**Database Setup Checklist - Complete**  
**Version:** 1.0  
**Created:** August 7, 2026  
**Status:** Ready to Execute  


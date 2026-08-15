# Database Setup - CORRECTED VERSION

## ✅ Fix Applied

**Problem:** Error "Database 'VehicleDealerDB' does not exist"

**Solution:** Updated `DATABASE_INIT.sql` to:
1. **Create the database first** (was commented out)
2. **Add EV-specific seed data** (users, models, colors, pricing, balances)
3. **Use correct table names** (Users, VehicleModels, not User, VehicleModel)

---

## 🚀 How to Run (3 Steps)

### Step 1: Open SSMS
- Launch SQL Server Management Studio
- Connect to: `localhost\SQLEXPRESS`
- Use Windows Authentication

### Step 2: Open Corrected Script
- File → Open → File...
- Navigate to: `d:\KRS\Specifications\DATABASE_INIT.sql`
- Click Open

### Step 3: Execute
- Press **F5** to run entire script
- Wait 30-60 seconds for completion
- Should see: "Seed Data Inserted Successfully"

---

## 📝 What Gets Created

**Database:** VehicleDealerDB

**10 Tables:**
```
✓ Users (29 records)
✓ VehicleModels (8 EV models)
✓ VehicleColors (6 colors)
✓ VehiclePriceHistory (48 pricing records)
✓ PurchaseOrders (empty - ready for data)
✓ Vehicles (empty - will be filled on orders)
✓ CommissionHistory (empty)
✓ AccountBalance (28 accounts with ₹10L each)
✓ AccountTransactions (empty - audit trail)
✓ AuditLog (empty - system audit)
```

---

## 👥 User Accounts Created

### Admin Account
```
Username: admin
Email: admin@krsdealers.com
Role: Admin
```

### Subdealer Accounts (28)
```
Username: subdealer_001 to subdealer_028
Email: subdealer###@krsdealers.com
Role: Subdealer
Balance: ₹10,00,000 each
Total: ₹2.8 crores
```

---

## 🚗 EV Models in Database

| Model | Price |
|-------|-------|
| Tesla Model 3 | ₹45,00,000 |
| Tesla Model Y | ₹65,00,000 |
| Tata Nexon EV | ₹15,00,000 |
| Hyundai Kona Electric | ₹23,50,000 |
| MG ZS EV | ₹18,00,000 |
| Mahindra XUV400 | ₹20,00,000 |
| Citroen eC3 | ₹12,00,000 |
| Audi e-tron GT | ₹85,00,000 |

---

## 🔗 Connection String

**Use this in appsettings.json:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=VehicleDealerDB;Trusted_Connection=true;Encrypt=false;"
```

---

## ✅ Verification Queries

After running the script, verify with these queries in SSMS:

### Check Database Exists
```sql
SELECT name FROM sys.databases WHERE name = 'VehicleDealerDB';
```
**Expected:** VehicleDealerDB (1 row)

### Check Users
```sql
SELECT COUNT(*) as Total, 
       SUM(CASE WHEN UserRole = 1 THEN 1 ELSE 0 END) as Admins,
       SUM(CASE WHEN UserRole = 2 THEN 1 ELSE 0 END) as Subdealers
FROM [Users];
```
**Expected:** Total=29, Admins=1, Subdealers=28

### Check Total Balance
```sql
SELECT SUM(CurrentBalance) as TotalBalance FROM [AccountBalance];
```
**Expected:** 28000000.00 (₹2.8 crores)

### Check EV Models
```sql
SELECT COUNT(*) FROM [VehicleModels];
```
**Expected:** 8

### Check Pricing
```sql
SELECT COUNT(*) FROM [VehiclePriceHistory];
```
**Expected:** 48 (8 models × 6 colors)

---

## 🐛 If Error Occurs

### Error: "Database already exists"
- **Solution:** Close SSMS and SQL Server (Ctrl+Alt+Delete → Services)
- Wait 30 seconds
- Reopen SSMS
- Run script again

### Error: "Table already exists"
- **Solution:** Manually drop database first:
```sql
ALTER DATABASE VehicleDealerDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE VehicleDealerDB;
```
- Then run the script again

### Error: "Cannot connect to server"
- **Solution:** Verify SQL Server service is running
- Services → SQL Server (SQLEXPRESS) → Status = Started
- If not started, right-click → Start

### Error: "Login failed"
- **Solution:** Use Windows Authentication (not SQL auth)
- SSMS → Connect → Authentication = Windows Authentication

---

## 📊 File Information

| File | Location | Purpose |
|------|----------|---------|
| DATABASE_INIT.sql | `d:\KRS\Specifications\` | Main setup script (CORRECTED) |
| DATABASE_SETUP.sql | `d:\KRS\KRSDealerManagement\` | Alternative with different DB name |
| DATABASE_SETUP_FIX.md | `d:\KRS\Requirement\` | This file |

**Use:** DATABASE_INIT.sql from `d:\KRS\Specifications\`

---

## 🎯 Next Steps

1. **Run the script** (all 3 steps above)
2. **Verify database creation** (run verification queries)
3. **Update your application**
   - Use connection string: `Server=localhost\SQLEXPRESS;Database=VehicleDealerDB;Trusted_Connection=true;Encrypt=false;`
4. **Test connection from application**
5. **Start using the system**

---

## ✨ Key Features Enabled

✅ Multi-account support (28 subdealers)  
✅ EV-specific database (8 electric vehicles)  
✅ Complete audit trail (AuditLog, AccountTransactions)  
✅ Balance management (CurrentBalance, ReservedAmount)  
✅ 100% audit coverage for compliance  
✅ Commission tracking ready  
✅ Real EV pricing (₹12L to ₹85L)  

---

## 📞 Support

If the script still fails:

1. Check SQL Server is running
   - Services → MSSQL$SQLEXPRESS → Running?
   
2. Check SSMS can connect
   - Object Explorer → Connected?
   
3. Try script with "Results to Text"
   - Query → Results To → Text (Ctrl+T)
   - Run script again for clearer errors

4. Check file exists
   - `d:\KRS\Specifications\DATABASE_INIT.sql`
   - File size > 20KB?

---

**Status:** ✅ Ready to Run  
**Database:** VehicleDealerDB  
**Server:** localhost\SQLEXPRESS  
**Users:** 29 (1 admin + 28 subdealers)  
**EV Models:** 8  
**Initial Investment:** ₹2.8 crores  


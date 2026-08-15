# KRS EV Dealer Management - Final Database Setup

## ✅ Database Name Confirmed: KRSDealerManagementDB

You prefer **KRSDealerManagementDB** - All scripts have been updated to use this naming.

---

## 📋 Available Setup Scripts

You have TWO options - both create the same database:

### Option 1: Full Reset (Recommended for Fresh Start)
**File:** `d:\KRS\KRSDealerManagement\DATABASE_SETUP.sql`
- Drops existing database
- Creates fresh database
- Adds all seed data (users, models, pricing, balances)
- Takes 30-60 seconds

### Option 2: Non-Destructive (Safe for Existing Data)
**File:** `d:\KRS\Specifications\DATABASE_INIT.sql`
- Uses IF NOT EXISTS for all objects
- Won't drop existing database
- Won't delete existing data
- Adds seed data only if tables are empty

---

## 🚀 How to Execute (Choose ONE)

### OPTION 1: Full Reset (Recommended)

**Step 1:** Open SSMS
- Server: `localhost\SQLEXPRESS`
- Authentication: Windows

**Step 2:** Open Script
```
File → Open → File...
Path: d:\KRS\KRSDealerManagement\DATABASE_SETUP.sql
```

**Step 3:** Execute
```
Press F5
Wait 30-60 seconds
See: "=== Database Setup Complete ==="
```

---

### OPTION 2: Safe Setup (Non-Destructive)

**Step 1:** Open SSMS
- Server: `localhost\SQLEXPRESS`
- Authentication: Windows

**Step 2:** Open Script
```
File → Open → File...
Path: d:\KRS\Specifications\DATABASE_INIT.sql
```

**Step 3:** Execute
```
Press F5
Wait 30-60 seconds
See: "Seed Data Inserted Successfully"
```

---

## ✨ What Gets Created (Both Options)

**Database:** KRSDealerManagementDB

**10 Tables:**
```
✓ User                          (singular - matches your schema)
✓ SubdealerAccount              (multi-account per subdealer)
✓ AccountPermission             (configurable per account)
✓ AccountBalance                (balance tracking)
✓ VehicleModel                  (8 EV models)
✓ VehicleColor                  (6 colors)
✓ VehiclePriceHistory           (pricing history)
✓ Vehicle                       (inventory)
✓ PurchaseOrder                 (order management)
✓ Commission                    (commission tracking)
✓ CommissionRate                (rate definitions)
✓ ReturnRequest                 (returns management)
✓ Payment                       (payment tracking)
✓ AccountTransaction            (transaction audit)
✓ AuditLog                      (system audit)
```

**Seed Data:**
- 1 Admin user (admin@krsdealers.com)
- 28 Subdealer users (subdealer_001 to subdealer_028)
- 8 EV models with realistic Indian pricing
- 6 vehicle colors
- 48 price records (8 × 6)
- 28 account balances (₹10,00,000 each = ₹2.8 crores)

---

## 🔗 Connection String (Update appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;"
  }
}
```

---

## 📊 Difference Between Scripts

| Feature | DATABASE_SETUP.sql | DATABASE_INIT.sql |
|---------|-------------------|------------------|
| **Location** | `d:\KRS\KRSDealerManagement\` | `d:\KRS\Specifications\` |
| **Database** | KRSDealerManagementDB | KRSDealerManagementDB |
| **Drop Existing** | YES (full reset) | NO (safe) |
| **Create DB** | Explicit CREATE | IF NOT EXISTS |
| **Seed Data** | Complete | Complete |
| **Use When** | Fresh setup | Add to existing |
| **Safety** | Destructive | Non-destructive |

**Recommendation:** Use DATABASE_SETUP.sql for initial setup (cleaner)

---

## ✅ Quick Verification

After running either script:

```sql
-- Check database
SELECT name FROM sys.databases WHERE name = 'KRSDealerManagementDB';

-- Check users (should be 29)
SELECT COUNT(*) FROM [User];

-- Check total balance (should be ₹2.8 crores)
SELECT SUM(CurrentBalance) FROM AccountBalance;

-- Check EV models (should be 8)
SELECT COUNT(*) FROM VehicleModel;
```

---

## 🚗 EV Models in Database

All prices in INR (August 2026):

| Model | Price | Commission |
|-------|-------|-----------|
| Tesla Model 3 | ₹45,00,000 | ₹8,000 |
| Tesla Model Y | ₹65,00,000 | ₹12,000 |
| Tata Nexon EV | ₹15,00,000 | ₹5,500 |
| Hyundai Kona Electric | ₹23,50,000 | ₹7,000 |
| MG ZS EV | ₹18,00,000 | ₹6,000 |
| Mahindra XUV400 | ₹20,00,000 | ₹6,500 |
| Citroen eC3 | ₹12,00,000 | ₹4,500 |
| Audi e-tron GT | ₹85,00,000 | ₹15,000 |

---

## 👥 User Accounts

### Admin
```
Username: admin
Email: admin@krsdealers.com
Role: Admin (1)
```

### Subdealers (28)
```
Username: subdealer_001 to subdealer_028
Email: subdealer###@krsdealers.com
Role: Subdealer (2)
Initial Balance: ₹10,00,000 each
```

---

## 📁 Key Files

| File | Location | Purpose |
|------|----------|---------|
| DATABASE_SETUP.sql (USE THIS) | `d:\KRS\KRSDealerManagement\` | Main setup - destructive |
| DATABASE_INIT.sql | `d:\KRS\Specifications\` | Alternative - safe |
| FINAL_DATABASE_SETUP.md | `d:\KRS\` | This guide |
| SETUP_CHECKLIST.md | `d:\KRS\` | Verification checklist |

---

## 🎯 Step-by-Step Execution

### For Fresh Setup (Recommended):

1. Open SSMS
2. File → Open → `d:\KRS\KRSDealerManagement\DATABASE_SETUP.sql`
3. Press F5
4. See success message
5. Done! ✅

### For Safe Setup:

1. Open SSMS
2. File → Open → `d:\KRS\Specifications\DATABASE_INIT.sql`
3. Press F5
4. See success message
5. Done! ✅

---

## 🐛 Troubleshooting

### Error: "Cannot connect to server"
```
→ Check SQL Server service running
→ Services → MSSQL$SQLEXPRESS = Started
```

### Error: "Database already exists"
```
→ Use DATABASE_SETUP.sql (drops and recreates)
→ Or manually drop:
  ALTER DATABASE KRSDealerManagementDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE KRSDealerManagementDB;
```

### Error: "Login failed"
```
→ Use Windows Authentication in SSMS
→ Not SQL Server authentication
```

### Script produces no output
```
→ Check Query → Results To = Text (Ctrl+T)
→ Run script again for error details
```

---

## ✨ Features Enabled

✅ **Multi-Account Support**
- Each subdealer has independent accounts
- Each account has separate balance & permissions
- Configurable menu access per account

✅ **EV Inventory Management**
- 8 electric vehicle models
- Real Indian market pricing
- 6 color options per model
- Monthly price history tracking

✅ **Complete Audit Trail**
- 100% operation logging
- WHO/WHAT/WHEN/WHY tracking
- Transaction history
- System audit log

✅ **Balance Management**
- CurrentBalance: actual funds
- ReservedAmount: locked for pending orders
- AvailableBalance: current - reserved
- Automatic tracking of all changes

✅ **Order & Commission**
- Purchase order workflow (Pending/Approved/Rejected)
- Commission tracking per vehicle per month
- Commission rate management
- Return request handling

✅ **Payment Processing**
- Payment submissions (Cash/GPay/NEFT/Others)
- Payment approval workflow
- Applied/Pending status tracking

---

## 📝 After Database Setup

### 1. Update Connection String
Edit `appsettings.json`:
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;"
```

### 2. Build Solution
```
Visual Studio → Build → Build Solution
```

### 3. Test Connection
Create test code to verify connection works

### 4. Run Application
```
F5 to start debugging
```

### 5. Login and Test
- Login as admin
- View subdealer accounts
- Create purchase order
- Verify audit trail

---

## 🔍 Database Structure

```
KRSDealerManagementDB
├── User (29 records)
│   ├── 1 Admin
│   └── 28 Subdealers
│
├── SubdealerAccount (28 records)
│   ├── Main account per subdealer
│   └── Independent permissions
│
├── AccountPermission (112 records)
│   ├── 4 permissions per account
│   └── Configurable access
│
├── AccountBalance (28 records)
│   └── ₹10L per account = ₹2.8 crores total
│
├── VehicleModel (8 records)
│   └── 8 EV models
│
├── VehicleColor (6 records)
│   └── 6 color options
│
├── VehiclePriceHistory (48 records)
│   └── 8 × 6 price combinations
│
└── Transaction Tables
    ├── PurchaseOrder (order management)
    ├── Vehicle (inventory)
    ├── Commission (commission tracking)
    ├── CommissionRate (rate definitions)
    ├── Payment (payment tracking)
    ├── ReturnRequest (returns)
    ├── AccountTransaction (audit trail)
    └── AuditLog (system audit)
```

---

## ⏱️ Execution Time

- **DATABASE_SETUP.sql:** 30-60 seconds (drops + creates + seeds)
- **DATABASE_INIT.sql:** 30-60 seconds (creates if not exists + seeds)

Both include:
- Table creation
- Index creation
- Seed data insertion
- Verification output

---

## 🎉 Status: READY FOR EXECUTION

**Database Name:** ✅ KRSDealerManagementDB  
**Scripts:** ✅ Both updated and ready  
**Seed Data:** ✅ Complete (29 users, 8 models, pricing)  
**Connection String:** ✅ Provided  
**Documentation:** ✅ Complete  

**Next Step:** Open SSMS and run DATABASE_SETUP.sql

---

**Last Updated:** August 7, 2026  
**Database:** KRSDealerManagementDB  
**Status:** ✅ Ready to Deploy  


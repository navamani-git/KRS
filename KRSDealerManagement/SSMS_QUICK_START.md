# SSMS Quick Start - Run Database Setup in 5 Minutes

## 🚀 Quick Steps

### Step 1: Open SSMS
- Launch **SQL Server Management Studio**
- Connect to: `localhost\SQLEXPRESS`
- Authentication: **Windows Authentication**
- Click **Connect**

### Step 2: Open Script File
- Menu → **File** → **Open** → **File...**
- Navigate to: `d:\KRS\KRSDealerManagement\DATABASE_SETUP.sql`
- Click **Open**

### Step 3: Execute Script
- Press **F5** or click **Execute** button
- ⏳ Wait 30-60 seconds

### Step 4: Verify Success
- Should see: `=== Database Setup Complete ===`
- Check Messages tab for confirmation

### Step 5: Verify Database
- In **Object Explorer** (left panel)
- Right-click **Databases** → **Refresh**
- Expand **KRSDealerManagementDB** → **Tables**
- Should see 15 tables

---

## ✅ What Gets Created

**Database:** KRSDealerManagementDB

**15 Tables:**
```
✓ User (29 records: 1 admin + 28 subdealers)
✓ SubdealerAccount (28 records)
✓ AccountPermission (112 records: 4 per account)
✓ AccountBalance (28 records)
✓ VehicleModel (8 EV models)
✓ VehicleColor (6 colors)
✓ VehiclePriceHistory (48 records)
✓ Vehicle (empty - will be filled on orders)
✓ PurchaseOrder (empty)
✓ Commission (empty)
✓ CommissionRate (8 records)
✓ ReturnRequest (empty)
✓ Payment (empty)
✓ AccountTransaction (empty)
✓ AuditLog (empty)
```

**Seed Data:**
- ₹2.8 crores total balance distributed
- 8 EV models (Tesla, Tata, Hyundai, MG, Mahindra, Citroen, Audi)
- 6 colors per model
- 8 commission rates
- 4 default permissions per account

---

## 🔐 Login Credentials

### Admin Account
```
Username: admin
Email: admin@krsdealers.com
Role: Admin (Full Access)
Phone: 9876543210
```

### Subdealer Accounts (28 total)
```
Username: subdealer_001 to subdealer_028
Email: subdealer###@krsdealers.com
Role: Subdealer (Limited Access)
Balance: ₹10,00,000 each
```

---

## 🔗 Connection String

**For Application (appsettings.json):**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;"
```

**For SSMS (if not already connected):**
```
Server: localhost\SQLEXPRESS
Authentication: Windows
Database: KRSDealerManagementDB
```

---

## 📊 Quick Data Checks

### Check Users Created
```sql
SELECT Username, UserRole FROM [User] ORDER BY UserId;
-- Expected: admin + 28 subdealers
```

### Check Total Balance
```sql
SELECT SUM(CurrentBalance) as TotalBalance FROM AccountBalance;
-- Expected: 28,00,00,000 (₹2.8 crores)
```

### Check EV Models
```sql
SELECT ModelName FROM VehicleModel ORDER BY ModelId;
-- Expected: 8 EV models
```

### Check Commission Rates
```sql
SELECT vm.ModelName, cr.CommissionAmount 
FROM CommissionRate cr
JOIN VehicleModel vm ON cr.ModelId = vm.ModelId
ORDER BY cr.CommissionAmount DESC;
-- Expected: 8 rates ranging from ₹4.5K to ₹15K
```

### Check Permissions Setup
```sql
SELECT COUNT(*) FROM AccountPermission;
-- Expected: 112 (28 accounts × 4 permissions)
```

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| "Cannot connect" | Check SQL Server service is running. Right-click SSMS → Run as Admin |
| "Login failed" | Use Windows Authentication, not SQL Auth |
| "Database already exists" | Script drops old DB automatically |
| "Table already exists" | Run fresh script, or drop database manually |
| "Script has errors" | Check file path and ensure file not open elsewhere |
| "Timeout" | Increase timeout: Query → Query Options → Execution → Timeout |

---

## 📝 Sample Workflows to Test

### Test 1: Check User Can Login
```sql
SELECT * FROM [User] WHERE Username = 'admin';
-- Should return admin record
```

### Test 2: View Subdealer Account
```sql
SELECT sa.*, ab.CurrentBalance 
FROM SubdealerAccount sa
JOIN AccountBalance ab ON sa.AccountId = ab.SubdealerAccountId
WHERE sa.SubdealerId = 2  -- First subdealer (admin is ID 1)
ORDER BY sa.AccountId;
```

### Test 3: Check Account Permissions
```sql
SELECT MenuName, IsAccessible, CanCreate, CanEdit, CanApprove
FROM AccountPermission
WHERE AccountId = 1;
-- Expected: 4 menus
```

### Test 4: List Available EV Models with Prices
```sql
SELECT DISTINCT vm.ModelName, vph.Price
FROM VehicleModel vm
JOIN VehiclePriceHistory vph ON vm.ModelId = vph.ModelId
WHERE vph.Month = 8 AND vph.Year = 2026
ORDER BY vph.Price DESC;
```

### Test 5: Commission Rates by Model
```sql
SELECT vm.ModelName, cr.CommissionAmount
FROM CommissionRate cr
JOIN VehicleModel vm ON cr.ModelId = vm.ModelId
WHERE cr.StartMonth = 8 AND cr.StartYear = 2026
ORDER BY cr.CommissionAmount DESC;
```

---

## 🎯 Next Steps After Setup

1. **Test Connection from Application**
   ```csharp
   // In your .NET application
   var connectionString = "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;";
   using (var connection = new SqlConnection(connectionString))
   {
       connection.Open(); // Should succeed
   }
   ```

2. **Create Admin Dashboard**
   - View all subdealers and balances
   - Manage vehicle models and prices
   - Track commissions and payments
   - Review audit logs

3. **Create Subdealer Dashboard**
   - View account balance
   - Create purchase orders
   - Submit commissions
   - Track transactions

4. **Test End-to-End Workflow**
   - Subdealer creates order → Admin approves → Balance updated → Audit logged

---

## 📚 EV Models in Database

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

## 📍 File Locations

| File | Location | Purpose |
|------|----------|---------|
| DATABASE_SETUP.sql | `d:\KRS\KRSDealerManagement\` | Main setup script |
| DATABASE_SETUP_INSTRUCTIONS.md | `d:\KRS\KRSDealerManagement\` | Detailed instructions |
| DATABASE_UPDATES.md | `d:\KRS\KRSDealerManagement\` | What changed (EV specific) |
| SSMS_QUICK_START.md | `d:\KRS\KRSDealerManagement\` | This file |

---

## ⚡ Performance Tips

- **First Run:** 30-60 seconds (drops old DB, creates new, seeds data)
- **Subsequent Runs:** ~45 seconds (fresh database)
- **Query Execution:** Use indexes on foreign keys and date fields
- **Audit Trail:** Very fast - simple inserts with timestamp

---

## 🔒 Security Notes

✅ **Implemented:**
- Windows Authentication (no SQL Server accounts)
- Encrypted passwords (hash stored, not plaintext)
- Foreign key constraints (data integrity)
- Indexes on sensitive fields (UserId, SubdealerId)

⚠️ **TODO:**
- Change default password hash after first login
- Set Encrypt=true in production connection string
- Enable SQL Server audit logging
- Set up database backups

---

## 📞 Support

**If script fails:**

1. Check `DATABASE_SETUP_OUTPUT.txt` for detailed errors (if created)
2. Try running in SSMS with "Results to Text" mode for clearer errors
3. Ensure SQL Server Express is running:
   - Services → SQL Server (SQLEXPRESS) → Status = Started
4. Check firewall isn't blocking SQL Server
5. Verify Windows user has SQL Server access rights

**Common Issues:**
- **"Login failed"** → Use Windows Auth, not SQL Auth
- **"Cannot connect"** → SQL Server service not running
- **"Database exists"** → Drop manually or close SSMS and retry

---

## ✨ Key Features Configured

✓ Multi-account support (28 subdealers with 28 accounts)  
✓ Configurable permissions (4 menus per account)  
✓ 100% audit trail (every operation tracked)  
✓ Balance management (current/reserved/available)  
✓ EV-specific pricing (8 models, 6 colors, 48 prices)  
✓ Commission tracking (8 rates per model)  
✓ Transaction history (complete accounting trail)  
✓ Compliance ready (WHO/WHAT/WHEN/WHY)

---

**Ready to Setup?**

1. Open SSMS
2. Open `DATABASE_SETUP.sql`
3. Press F5
4. Wait 60 seconds
5. Done! ✅

---

*Updated: August 7, 2026*  
*Database Version: 1.0*  
*EV Models: 8*  
*Initial Subdealers: 28*


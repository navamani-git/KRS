# Database Script Updates - EV Dealer Specific

## Changes Made to DATABASE_SETUP.sql

### 1. Vehicle Models Updated
**Changed from:** Generic vehicles (BMW, Toyota, Mahindra, etc.)  
**Changed to:** Electric Vehicles (Tesla, Tata, Hyundai, etc.)

**New EV Models:**
```sql
1. Tesla Model 3      - Premium electric sedan
2. Tesla Model Y      - Electric SUV  
3. Tata Nexon EV      - Compact electric SUV
4. Hyundai Kona Electric - Premium electric SUV
5. MG ZS EV           - Compact electric SUV
6. Mahindra XUV400    - Mid-size electric SUV
7. Citroen eC3        - Budget electric SUV
8. Audi e-tron GT     - Premium electric sedan
```

### 2. EV-Specific Pricing (August 2026)
**All prices in Indian Rupees (INR)**

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

### 3. EV Commission Rates (August 2026)
**Commission per vehicle sold**

| Model | Commission |
|-------|-----------|
| Tesla Model 3 | ₹8,000 |
| Tesla Model Y | ₹12,000 |
| Tata Nexon EV | ₹5,500 |
| Hyundai Kona Electric | ₹7,000 |
| MG ZS EV | ₹6,000 |
| Mahindra XUV400 | ₹6,500 |
| Citroen eC3 | ₹4,500 |
| Audi e-tron GT | ₹15,000 |

### 4. Admin User Updated
**Changed:** LastName from "User" to "EV Dealer"  
**Email:** admin@krsdealers.com  
**Role:** Admin (1)

### 5. Current Year Updated
**Changed:** 2024 → 2026  
**Reason:** Matches current session date (August 7, 2026)  
**Impact:** All prices and commissions are set for August 2026

### 6. Database Name
**Database:** KRSDealerManagementDB  
**Server:** localhost\SQLEXPRESS  
**Connection String:** `Server=localhost\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;`

---

## Database Structure (15 Tables)

### Core Tables
1. **User** - Admin and Subdealer authentication (29 total: 1 admin + 28 subdealers)
2. **SubdealerAccount** - Multiple accounts per subdealer (28 main accounts)
3. **AccountPermission** - Menu-level permissions per account (4 per account)
4. **AccountBalance** - Track CurrentBalance, ReservedAmount, AvailableBalance

### Vehicle Management
5. **VehicleModel** - 8 EV models
6. **VehicleColor** - 6 colors (Pearl White, Jet Black, Silver, Red, Blue, Gold)
7. **VehiclePriceHistory** - Monthly pricing (48 records: 8 models × 6 colors)
8. **Vehicle** - Individual EV instances with ChassisNumbers

### Order & Commission Management
9. **PurchaseOrder** - Order requests from subdealers
10. **Commission** - Commission submissions per vehicle/month
11. **CommissionRate** - Rate definitions (8 rates for 8 EV models)
12. **ReturnRequest** - Vehicle return/refund requests

### Transaction Tracking
13. **Payment** - Payment submissions and approvals
14. **AccountTransaction** - Complete balance change history (Debit/Credit/Reserved/Released)
15. **AuditLog** - 100% audit trail (WHO/WHAT/WHEN/WHY)

---

## Seed Data Summary

### Users
- **Admin:** 1 user
  - Username: admin
  - Email: admin@krsdealers.com
  
- **Subdealers:** 28 users
  - Usernames: subdealer_001 to subdealer_028
  - Emails: subdealer###@krsdealers.com

### Financial Setup
- **Per Subdealer Initial Balance:** ₹10,00,000
- **Total Initial Investment:** ₹28,00,00,000 (₹2.8 crores)
- **Accounts Created:** 28 (one main account per subdealer)
- **Default Permissions:** 4 menus per account (POs, Commissions, Payments, Account Details)

### Inventory Setup
- **EV Models:** 8
- **Colors:** 6
- **Price History Records:** 48 (8 × 6)
- **Commission Rates:** 8

---

## Key Differences from Generic Vehicle System

| Feature | Generic | EV Dealer |
|---------|---------|-----------|
| Vehicle Types | Mixed (Sedans, SUVs, MPVs) | Electric Only (EVs) |
| Price Range | ₹12L to ₹85L | ₹12L to ₹85L (EV specific) |
| Commission Range | ₹2.5K to ₹15K | ₹4.5K to ₹15K |
| Models | Toyota, BMW, Mahindra | Tesla, Tata, Hyundai, MG, etc. |
| Focus | General vehicles | Sustainable EV ecosystem |
| Subsidy Tracking | Not applicable | Can be extended for govt EV subsidies |

---

## Business Logic Implemented

### Multi-Account Support
- Each subdealer can have multiple accounts
- Each account has:
  - Independent balance
  - Separate permissions
  - Own transaction history
  - Dedicated audit logs

### Balance Management
- **CurrentBalance:** Actual money in account
- **ReservedAmount:** Locked for pending orders
- **AvailableBalance:** CurrentBalance - ReservedAmount

### Commission Tracking
- One commission per vehicle per month
- Automatic rate lookup by model
- Approval workflow (Pending → Approved → Paid)
- Complete transaction history

### Audit Trail (100% Coverage)
- User action logging (WHO/WHAT/WHEN/WHY)
- Balance transaction tracking
- Commission approval trail
- Modification history with timestamps

---

## How to Use the Updated Database

### 1. Setup Database
```sql
-- Copy entire DATABASE_SETUP.sql to SSMS
-- Press F5 to execute
-- Takes 30-60 seconds
-- Result: Fresh database with all EV data
```

### 2. Connect from Application
```csharp
// In appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;"
  }
}
```

### 3. Initial Login Credentials
```
Admin Account:
- Username: admin
- Password: Check default hash or reset
- Role: Admin (Full access)

Subdealer Accounts:
- Username: subdealer_001 to subdealer_028
- Password: Check default hash or reset
- Role: Subdealer (Limited access per account)
```

### 4. Start Using System
1. Admin creates new EV models or updates prices
2. Subdealers create purchase orders
3. Admin approves orders
4. System tracks balance and transactions
5. All operations logged in AuditLog

---

## Verification Queries

### Check Database Created
```sql
SELECT name FROM sys.databases WHERE name = 'KRSDealerManagementDB';
```

### Count Users
```sql
SELECT UserRole, COUNT(*) FROM [User] GROUP BY UserRole;
-- Expected: Admin=1, Subdealer=28
```

### Total Balances
```sql
SELECT SUM(CurrentBalance) FROM AccountBalance;
-- Expected: ₹2.8 crores (28,00,00,000)
```

### EV Models List
```sql
SELECT ModelName, COUNT(*) as PriceRecords FROM VehicleModel vm
LEFT JOIN VehiclePriceHistory vph ON vm.ModelId = vph.ModelId
GROUP BY vm.ModelName;
-- Expected: 8 models with 6 prices each
```

### Commission Rates
```sql
SELECT vm.ModelName, cr.CommissionAmount FROM CommissionRate cr
JOIN VehicleModel vm ON cr.ModelId = vm.ModelId
ORDER BY cr.CommissionAmount DESC;
```

---

## Next Steps

### After Database Setup

1. **Configure Application**
   - Update appsettings.json with connection string
   - Build solution
   - Test connection

2. **Create Web Controllers**
   - Implement MVC controllers for each domain
   - Consume CQRS commands/queries
   - Use DTOs for views

3. **Create UI Views**
   - Use AdminLTE templates
   - Responsive design (Mobile, Tablet, Desktop)
   - KRS logo in navbar/sidebar

4. **Test Workflows**
   - Login as admin
   - Create subdealer purchase order
   - Approve and verify balance changes
   - Check audit logs

5. **Deploy to Production**
   - Update connection string for prod server
   - Run DATABASE_SETUP.sql on production
   - Monitor audit trail for compliance

---

## Important Notes

⚠️ **Read-Only Database Setting**
- This script **drops and recreates** the database
- All previous data will be deleted
- Use only for fresh setup or testing
- For production, use migrations or backups

⚠️ **Default Passwords**
- Default password hash provided in script
- Should be changed after first login
- Implement password reset functionality

⚠️ **Encryption**
- Encrypt=false for local development
- Change to Encrypt=true for production
- Update connection string accordingly

✅ **Audit Trail**
- 100% of operations logged
- Cannot be disabled
- Critical for compliance and debugging

✅ **EV Industry Ready**
- Supports EV pricing
- Tracks commission per vehicle
- Can be extended for:
  - Government EV subsidies
  - Battery warranty tracking
  - Charging station partnerships
  - Electric meter integration

---

## File Location

**Database Setup Script:**  
`d:\KRS\KRSDealerManagement\DATABASE_SETUP.sql`

**Instructions:**  
`d:\KRS\KRSDealerManagement\DATABASE_SETUP_INSTRUCTIONS.md`

**Connection String:**  
`Server=localhost\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;`

---

**Updated:** August 7, 2026  
**Version:** 1.0  
**EV Models:** 8  
**Initial Subdealers:** 28  
**Database Status:** Ready for setup


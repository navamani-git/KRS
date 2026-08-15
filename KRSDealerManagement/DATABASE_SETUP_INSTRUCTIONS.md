# KRS EV Dealer Management - Database Setup Instructions

## Step-by-Step Guide

### Prerequisites
- SQL Server Management Studio (SSMS) installed
- SQL Server Express (localhost\SQLEXPRESS) running
- Connection permissions to local SQL Server

### Steps to Setup Database

#### 1. Open SQL Server Management Studio (SSMS)
   - Launch SSMS
   - Server name: `localhost\SQLEXPRESS`
   - Authentication: Windows Authentication
   - Click **Connect**

#### 2. Open the Database Setup Script
   - File → Open → File...
   - Navigate to: `d:\KRS\KRSDealerManagement\DATABASE_SETUP.sql`
   - Click **Open**

#### 3. Execute the Script
   - Press **F5** or click **Execute** button
   - Wait for completion (should take 30-60 seconds)

#### 4. Verify Database Creation
   - In Object Explorer, right-click on **Databases**
   - Click **Refresh**
   - You should see `KRSDealerManagementDB` in the list

#### 5. Check Tables Were Created
   - Expand `KRSDealerManagementDB`
   - Expand `Tables`
   - Verify all 15 tables exist:
     - User
     - SubdealerAccount
     - AccountPermission
     - AccountBalance
     - VehicleModel
     - VehicleColor
     - VehiclePriceHistory
     - Vehicle
     - PurchaseOrder
     - Commission
     - CommissionRate
     - ReturnRequest
     - Payment
     - AccountTransaction
     - AuditLog

#### 6. Verify Seed Data
   - Right-click `User` table
   - Select **Edit Top 200 Rows**
   - Verify admin user and 28 subdealers created
   - Expected: 29 rows total (1 admin + 28 subdealers)

#### 7. Test Connection from Application

In your `appsettings.json`, verify the connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;"
  }
}
```

---

## Database Overview

### Connection Details
- **Server:** localhost\SQLEXPRESS
- **Database:** KRSDealerManagementDB
- **Authentication:** Windows (Integrated Security)
- **Encrypt:** false (for development)

### Tables Created (15)
| Table | Purpose |
|-------|---------|
| User | Admin and Subdealer users |
| SubdealerAccount | Multiple accounts per subdealer |
| AccountPermission | Configurable menu permissions |
| AccountBalance | Track balance, reserved amount, available balance |
| VehicleModel | EV models (Tesla, Tata, Hyundai, etc.) |
| VehicleColor | Available colors |
| VehiclePriceHistory | Monthly pricing per model/color |
| Vehicle | Individual vehicle records |
| PurchaseOrder | Order requests from subdealers |
| Commission | Commission tracking per vehicle/month |
| CommissionRate | Commission rate definitions |
| ReturnRequest | Vehicle return/refund requests |
| Payment | Payment submissions |
| AccountTransaction | Complete transaction history |
| AuditLog | System-wide audit trail |

### Seed Data Inserted

#### Users (29 total)
- **1 Admin User**
  - Username: `admin`
  - Email: `admin@krsdealers.com`
  - Role: Admin
  - Phone: 9876543210

- **28 Subdealer Users**
  - Usernames: `subdealer_001` to `subdealer_028`
  - Emails: `subdealer001@krsdealers.com` to `subdealer028@krsdealers.com`
  - Role: Subdealer
  - Initial Balance: ₹10,00,000 each

#### Vehicle Models (8 EV Models)
1. Tesla Model 3 - ₹45,00,000
2. Tesla Model Y - ₹65,00,000
3. Tata Nexon EV - ₹15,00,000
4. Hyundai Kona Electric - ₹23,50,000
5. MG ZS EV - ₹18,00,000
6. Mahindra XUV400 - ₹20,00,000
7. Citroen eC3 - ₹12,00,000
8. Audi e-tron GT - ₹85,00,000

#### Vehicle Colors (6)
- Pearl White
- Jet Black
- Silver
- Red
- Blue
- Gold

#### Commission Rates (August 2026)
- Tesla Model 3: ₹8,000
- Tesla Model Y: ₹12,000
- Tata Nexon EV: ₹5,500
- Hyundai Kona Electric: ₹7,000
- MG ZS EV: ₹6,000
- Mahindra XUV400: ₹6,500
- Citroen eC3: ₹4,500
- Audi e-tron GT: ₹15,000

#### Subdealer Accounts (28)
- Each subdealer gets 1 Main account
- Account Type: Sales
- Initial Balance: ₹10,00,000 each
- Total invested: ₹2.8 crores

#### Account Permissions (4 per account)
- Purchase Orders (Full access)
- Commissions (Full access)
- Payments (Full access)
- Account Details (View only)

---

## Troubleshooting

### Error: "Cannot connect to server"
- Ensure SQL Server service is running
- Check server name: Should be `localhost\SQLEXPRESS`
- Verify Windows Authentication is enabled

### Error: "Database already exists"
- The script drops and recreates the database
- This is intentional for a fresh setup
- If you want to keep existing data, comment out lines 13-16

### Error: "Login failed"
- Use Windows Authentication (not SQL Server authentication)
- Ensure you have local admin rights
- Check firewall isn't blocking SQL Server

### Error: "Table already exists"
- Run the script from scratch (drops/recreates)
- Or manually drop the existing database:
  ```sql
  ALTER DATABASE KRSDealerManagementDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE KRSDealerManagementDB;
  ```

---

## Next Steps

1. **Create Initial Admin Credentials**
   - Change password from default hash
   - Update first login in application

2. **Update appsettings.json**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KRSDealerManagementDB;Trusted_Connection=true;Encrypt=false;"
     }
   }
   ```

3. **Create Subdealer Logins**
   - Each subdealer (subdealer_001 to subdealer_028) can now log in
   - They will see their account with ₹10,00,000 balance

4. **Start Creating Purchase Orders**
   - Admin can create vehicle models and prices
   - Subdealers can submit purchase orders
   - System tracks all transactions and audit trail

5. **Test Workflows**
   - Create purchase order → Approve → Verify balance
   - Submit commission → Approve → Track payment
   - All operations logged in AuditLog table

---

## Key Features Enabled

✅ **Multi-Account Support** - Each subdealer has separate accounts with independent balances

✅ **Configurable Permissions** - Admin can restrict access per menu per account

✅ **100% Audit Trail** - WHO/WHAT/WHEN/WHY tracked for all operations

✅ **Balance Management** - CurrentBalance, ReservedAmount, AvailableBalance tracking

✅ **EV Specific** - 8 electric vehicle models with realistic Indian pricing

✅ **Commission System** - Automatic tracking per vehicle per month

✅ **Transaction History** - Complete accounting trail for compliance

---

## Database Diagram

```
┌─────────────┐
│    User     │
│  (Admin/SD) │
└──────┬──────┘
       │
       ├─→ SubdealerAccount (1:M)
       │   ├─→ AccountBalance (1:1)
       │   ├─→ AccountPermission (1:M)
       │   └─→ PurchaseOrder (1:M)
       │
       ├─→ VehicleModel (1:M)
       │   ├─→ VehicleColor (M:M via PriceHistory)
       │   ├─→ VehiclePriceHistory (1:M)
       │   └─→ CommissionRate (1:M)
       │
       ├─→ PurchaseOrder (1:M)
       │   └─→ Vehicle (1:M)
       │
       ├─→ Commission (1:M)
       │   └─→ Vehicle (M:1)
       │
       └─→ AuditLog (1:M)

AccountTransaction (summary of all balance changes)
ReturnRequest (vehicle returns)
Payment (payment tracking)
```

---

## Support

If you encounter issues:

1. Check SQL Server is running: Services → MSSQL$SQLEXPRESS
2. Verify connection string in appsettings.json
3. Run script in SSMS with "Results to Text" for detailed error messages
4. Check DATABASE_SETUP.sql for any syntax errors

---

**Database Version:** 1.0  
**Created:** August 7, 2026  
**EV Models:** 8  
**Initial Subdealers:** 28  
**Initial Investment:** ₹2.8 crores


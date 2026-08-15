# KRS Dealer Management — Technical Guide

> **Purpose:** Step-by-step reference for manual development without AI.  
> **Project path:** `d:\KRS\KRSDealerManagement`  
> **Last updated:** August 2026

---

## Table of Contents

1. [Solution Overview](#1-solution-overview)
2. [Request Flow (How Code Runs)](#2-request-flow-how-code-runs)
3. [User Roles](#3-user-roles)
4. [Session — Complete Reference](#4-session--complete-reference)
5. [Menu System — Complete Reference](#5-menu-system--complete-reference)
6. [Authorization Filters](#6-authorization-filters)
7. [Data Scoping (Who Sees What Data)](#7-data-scoping-who-sees-what-data)
8. [Database Tables for Access Control](#8-database-tables-for-access-control)
9. [Sidebar Menu Map (UI → Controller)](#9-sidebar-menu-map-ui--controller)
10. [Controller Access Matrix](#10-controller-access-matrix)
11. [Step-by-Step: Add a New Screen](#11-step-by-step-add-a-new-screen)
12. [Step-by-Step: Add a New Menu](#12-step-by-step-add-a-new-menu)
13. [Step-by-Step: Change Who Can Access a Feature](#13-step-by-step-change-who-can-access-a-feature)
14. [Step-by-Step: Database Changes](#14-step-by-step-database-changes)
15. [Application Layer (Commands / Queries / Handlers)](#15-application-layer-commands--queries--handlers)
16. [Repository Pattern & Live DB](#16-repository-pattern--live-db)
17. [Key Business Features](#17-key-business-features)
18. [Helpers & Utilities](#18-helpers--utilities)
19. [Deployment](#19-deployment)
20. [Production Errors & Fixes](#20-production-errors--fixes)
21. [Test Logins](#21-test-logins)
22. [File Index (Where to Look)](#22-file-index-where-to-look)

---

## 1. Solution Overview

### Projects

| Project | Folder | Purpose |
|---------|--------|---------|
| **Web** | `KRSDealerManagement.Web` | MVC UI — Controllers, Views, Filters, Helpers |
| **Application** | `KRSDealerManagement.Application` | Business logic — Commands, Queries, Handlers, Services, Validators |
| **Domain** | `KRSDealerManagement.Domain` | Entities, repository interfaces |
| **Infrastructure** | `KRSDealerManagement.Infrastructure` | Dapper repositories, UnitOfWork, DB connection |
| **Shared** | `KRSDealerManagement.Shared` | Enums, constants (`MenuKeys`, `RoleCodes`, `StaffMenuAccess`) |

### Startup (`Web/Program.cs`)

```
Program.cs
  ├── AddControllersWithViews()
  ├── AddApplicationServices()     → MediatR, FluentValidation, AutoMapper, AuditService, VehiclePriceService
  ├── AddInfrastructureServices()  → ApplicationDbContext, UnitOfWork
  └── AddSession()                 → 30 min idle timeout
```

Connection string key: **`DefaultConnection`** (from `appsettings.json` or `web.config` env vars on IIS).

---

## 2. Request Flow (How Code Runs)

```
Browser HTTP request
    ↓
Controller action          (Web/Controllers/*.cs)
    ↓
[AuthorizeRole] filter     → checks role ID in session
[AuthorizeMenu] filter     → checks menu key in session
    ↓
_mediator.Send(Command)    or   _mediator.Send(Query)
    ↓
Handler                    (Application/Handlers/Commands or Queries)
    ↓
IUnitOfWork → Repository   (Infrastructure/Repositories)
    ↓
Dapper SQL → SQL Server
    ↓
View (.cshtml) returned to browser
```

### Example: Create Vehicle Price

| Step | File | What happens |
|------|------|--------------|
| 1 | `PricesController.Create` POST | Receives form, builds `CreateVehiclePriceCommand` |
| 2 | `CreateVehiclePriceCommandHandler` | Validates, inserts `VehiclePriceHistory` |
| 3 | `VehiclePriceService.ApplyCatalogPriceRevisionAsync` | Updates vehicles + account balances |
| 4 | `AuditService.LogActionAsync` | Writes audit log |

### MediatR auto-registration

All handlers in `Application/Handlers/` are registered automatically.  
No manual DI registration needed per handler.

Validators in `Application/Validators/` run via `ValidationBehavior<,>` pipeline.

---

## 3. User Roles

### Role IDs (legacy int — used in `[AuthorizeRole]`)

| ID | Enum name | RoleCode (DB) | Display name | Scope |
|----|-----------|---------------|--------------|-------|
| **1** | `Admin` | `SYSTEM_ADMIN` | System Admin | All dealerships |
| **2** | `Subdealer` | `SUBDEALER` | Subdealer | Own data only |
| **3** | `FinanceAdmin` | `FINANCE_ADMIN` | Finance Admin | All dealerships (finance screens) |
| **4** | `DealerBranchManager` | `BRANCH_MANAGER` | Branch Manager | One dealership + its subdealers |

**Source files:**
- `Shared/Enums/UserRoleEnum.cs`
- `Shared/Constants/RoleCodes.cs`
- DB table: `Roles`

### Hierarchy (`UserOrgRole` table)

| Role | DealershipId | SubDealerId |
|------|--------------|-------------|
| System Admin | `NULL` | `NULL` |
| Finance Admin | `NULL` or set | `NULL` |
| Branch Manager | Set (e.g. Karur) | `NULL` |
| Subdealer | Set | Set |

**Important:** For subdealers, `SubdealerId` on orders/accounts/vehicles = **`Users.UserId`** (login user ID), not `SubDealers.SubDealerId`.

---

## 4. Session — Complete Reference

### Where session is set

**File:** `Web/Controllers/AccountController.cs` → `Login` POST  
**Called after:** `LoginCommandHandler` returns success  
**Method:** `SessionHelper.SetUserSession(...)`

### Session keys stored

| Session key (internal) | Method to read | Description |
|------------------------|----------------|-------------|
| `UserId` | `GetUserId()` | Login user ID |
| `Username` | `GetUsername()` | Login username |
| `FullName` | `GetFullName()` | Display name |
| `UserRole` | `GetUserRole()` | Legacy int role (1–4) |
| `RoleName` | `GetRoleName()` | e.g. "System Admin" |
| `RoleCode` | `GetRoleCode()` | e.g. `SYSTEM_ADMIN` |
| `DealershipId` | `GetDealershipId()` | Branch scope (null for system admin) |
| `DealershipName` | `GetDealershipName()` | e.g. "Karur" |
| `SubDealerId` | `GetSubDealerId()` | Org subdealer ID (not same as UserId) |
| `AccessibleMenus` | via `HasMenuAccess()` | Comma-separated menu keys |

**Source file:** `Web/Helpers/SessionHelper.cs`

### Session helper methods

```csharp
// Authentication
SessionHelper.IsAuthenticated(session)
SessionHelper.ClearSession(session)

// Role checks
SessionHelper.IsSystemAdmin(session)    // Role 1 / SYSTEM_ADMIN
SessionHelper.IsSubdealer(session)        // Role 2 / SUBDEALER
SessionHelper.IsFinanceAdmin(session)     // Role 3 / FINANCE_ADMIN
SessionHelper.IsBranchManager(session)    // Role 4 / BRANCH_MANAGER
SessionHelper.IsStaff(session)            // Admin OR Finance OR Branch Manager

// Data scope
SessionHelper.GetDealershipScope(session)
// Returns NULL for system admin (= see all dealerships)
// Returns DealershipId for branch manager

// Menu check
SessionHelper.HasMenuAccess(session, menuKey)
// System admin → always TRUE
// Others → checks AccessibleMenus in session
```

### How menus get into session at login

**File:** `Application/Handlers/Commands/LoginCommandHandler.cs`

```
1. Find user by username
2. Verify password
3. Load UserOrgRoles → get RoleId, DealershipId, SubDealerId
4. Load Roles → get RoleCode, RoleName
5. Load RoleMenus WHERE RoleId = X AND IsAccessible = 1
6. (Subdealer only) Intersect with AccountPermissions if rows exist
7. Store menu keys in session via SetUserSession
```

### Refresh session after permission change

**File:** `Web/Helpers/PermissionHelper.cs` → `RefreshSessionAsync`

Call this after admin changes subdealer permissions so user sees updated menus without re-login.

```csharp
await PermissionHelper.RefreshSessionAsync(HttpContext, _mediator);
```

Uses `GetUserAccessContextQuery` to reload menus from DB.

### Session timeout

Configured in `Program.cs`: **30 minutes** idle timeout.

---

## 5. Menu System — Complete Reference

There are **TWO separate menu systems**.

### System A — Staff menus (Admin / Finance / Branch Manager)

**Constant file:** `Shared/Constants/StaffMenuAccess.cs`  
**DB table:** `RoleMenus` (linked to `Roles.RoleId`)  
**Session check:** `SessionHelper.HasMenuAccess(session, StaffMenuAccess.Xxx)`  
**Controller attribute:** `[AuthorizeMenu(StaffMenuAccess.Xxx)]`  
**Sidebar file:** `Web/Views/Shared/_Layout.cshtml` (staff section, `isStaff` block)

#### All staff menu keys

| Constant | MenuKey string | Sidebar label | Controller |
|----------|----------------|---------------|------------|
| `Dealers` | `admin_dealerships` | Dealerships | DealershipsController |
| `VehicleModels` | `admin_vehicle_models` | Vehicle Models | VehicleModelsController |
| `VehicleColors` | `admin_vehicle_colors` | Vehicle Colors | VehicleColorsController |
| `Prices` | `admin_prices` | Price Management | PricesController |
| `FinanceNames` | `admin_finance_names` | Finance Names | FinanceNamesController |
| `DocumentTypes` | `admin_document_types` | Document Types | DocumentTypesController |
| `RtoLocations` | `admin_rto_locations` | RTO Locations | RtoLocationsController |
| `StatusLookups` | `admin_status_lookups` | Status Master | StatusLookupsController |
| `Subdealers` | `admin_subdealers` | Subdealers | SubdealersController |
| `Balances` | `admin_balances` | Balances | AccountsController |
| `CommissionRates` | `admin_commission_rates` | Commission Rates | CommissionsController |
| `Orders` | `admin_orders` | Manage Orders | OrdersController |
| `Vehicles` | `admin_vehicles` | Subdealer Vehicles | VehiclesController |
| `VehicleBookings` | `admin_vehicle_bookings` | Vehicle Bookings | VehicleBookingsController |
| `Returns` | `admin_returns` | Return Requests | ReturnsController |
| `Payments` | `admin_payments` | Payment Approvals | PaymentsController |
| `Reports` | `admin_reports` | Reports | ReportsController |
| `StaffUsers` | `admin_staff_users` | Staff Users | (if implemented) |

#### Default access by role (code reference — `StaffMenuAccess.CanAccess`)

| Menu | Admin (1) | Finance (3) | Branch Mgr (4) |
|------|-----------|-------------|----------------|
| Dealerships, Models, Colors, Prices, Finance, Docs, RTO, Status | ✅ | ❌ | ❌ |
| Subdealers, Orders, Vehicles, Bookings, Returns | ✅ | ❌ | ✅ |
| Balances, Payments, Reports | ✅ | ✅ | ❌ |
| Commission Rates | ✅ | ❌ | ❌ |

> **Note:** Actual runtime access comes from **`RoleMenus` DB rows** loaded at login.  
> `StaffMenuAccess.CanAccess` is the intended default — ensure DB matches.

---

### System B — Subdealer menus

**Constant file:** `Shared/Constants/MenuKeys.cs`  
**DB tables:** `RoleMenus` (subdealer role) + `AccountPermissions` (per account override)  
**Session check:** `PermissionHelper.HasAccess(session, MenuKeys.Xxx)` (= `SessionHelper.HasMenuAccess`)  
**Controller attribute:** `[AuthorizeMenu(MenuKeys.Xxx)]`  
**Sidebar file:** `_Layout.cshtml` (subdealer section, `isSubdealer` block)  
**Admin config UI:** Subdealers → Details → Configure Permissions

#### All subdealer menu keys

| Constant | MenuKey string | Sidebar label | Controller / Action |
|----------|----------------|---------------|---------------------|
| `AccountStatements` | `account_statements` | Account Statement | Account/Statement |
| `PurchaseOrderCreate` | `purchase_orders_create` | Create Order | Orders/Create |
| `PurchaseOrderView` | `purchase_orders_view` | My Orders | Orders/MyOrders |
| `VehiclesView` | `vehicles_view` | My Vehicles | Vehicles/Index |
| `CommissionSubmit` | `commissions_submit` | Submit Commission | Commissions/Submit |
| `MyPayments` | `my_payments` | My Payments | Payments/MyPayments |
| `Reports` | `reports` | Reports | Reports/Index |

#### Configurable menus list (admin UI)

**Method:** `MenuKeys.GetSubdealerConfigurableMenus()`  
Returns the 7 menus above with default `IsAccessible = true`.

When creating a subdealer (`CreateSubdealerCommandHandler`):
1. Admin checks/unchecks menus on Create form
2. Rows inserted into `AccountPermissions` per menu key
3. At login, `RoleMenus` loaded first, then filtered by `AccountPermissions`

**If `AccountPermissions` has NO rows** → all `RoleMenus` for subdealer role apply.  
**If `AccountPermissions` HAS rows** → only menus with `IsAccessible = 1` remain.

---

## 6. Authorization Filters

### `[AuthorizeRole(1, 2, 4)]`

**File:** `Web/Filters/AuthorizeRoleAttribute.cs`

- Checks user is logged in → else redirect to `/Account/Login`
- Checks `SessionHelper.GetUserRole()` is in allowed list → else `/Account/AccessDenied`

**Usage examples:**
```csharp
[AuthorizeRole(1)]          // Admin only
[AuthorizeRole(2)]          // Subdealer only
[AuthorizeRole(1, 4)]       // Admin + Branch Manager
[AuthorizeRole(1, 2, 3, 4)] // All roles
```

### `[AuthorizeMenu("menu_key")]`

**File:** `Web/Helpers/PermissionHelper.cs` (class `AuthorizeMenuAttribute`)

- Checks user is logged in
- Checks `SessionHelper.HasMenuAccess(session, menuKey)` → else AccessDenied
- System admin always passes

**Always use BOTH filters when needed:**
```csharp
[AuthorizeRole(2)]
[AuthorizeMenu(MenuKeys.CommissionSubmit)]
public IActionResult Submit() { ... }
```

Role = who you are. Menu = what feature you can use.

---

## 7. Data Scoping (Who Sees What Data)

### System Admin (role 1)
```csharp
SessionHelper.GetDealershipScope(session)  // returns NULL
// Queries: no DealershipId filter → sees ALL data
```

### Branch Manager (role 4)
```csharp
SessionHelper.GetDealershipScope(session)  // returns their DealershipId
// Filter subdealers: WHERE DealershipId = scope
// Filter orders/bookings/vehicles: via subdealer IDs in that dealership
```

### Finance Admin (role 3)
```csharp
// Typically all dealerships for balances/payments
// Uses role-based controller access, not always dealership filter
```

### Subdealer (role 2)
```csharp
query.SubdealerId = SessionHelper.GetUserId(session)
// Sees ONLY their own orders, vehicles, commissions, payments
```

### Pattern in controllers

```csharp
var query = new GetVehiclesQuery();
if (SessionHelper.IsSubdealer(HttpContext.Session))
    query.SubdealerId = userId;
else
    query.DealershipId = SessionHelper.GetDealershipScope(HttpContext.Session);
```

---

## 8. Database Tables for Access Control

### `Roles`
```
RoleId, RoleCode, RoleName, IsActive
```
Codes: `SYSTEM_ADMIN`, `BRANCH_MANAGER`, `FINANCE_ADMIN`, `SUBDEALER`

### `UserOrgRoles`
```
UserOrgRoleId, UserId, RoleId, DealershipId, SubDealerId, IsPrimary, IsActive
```
Links login user to role + location.

### `RoleMenus`
```
RoleMenuId, RoleId, MenuKey, MenuName, IsAccessible, SortOrder
```
**This is what populates session menus at login.**

Example — give Branch Manager vehicle bookings:
```sql
INSERT INTO RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
SELECT RoleId, 'admin_vehicle_bookings', 'Vehicle Bookings', 1, 50
FROM Roles WHERE RoleCode = 'BRANCH_MANAGER';
```

### `AccountPermissions` (subdealer only)
```
PermissionId, AccountId, MenuKey, MenuName, IsAccessible,
CanCreate, CanEdit, CanDelete, CanApprove
```
Configured via: **Subdealers → Details → Configure Permissions**

### `SubdealerAccounts` + `AccountBalances`
Each subdealer gets a "Main Account" wallet on creation.

---

## 9. Sidebar Menu Map (UI → Controller)

**File:** `Web/Views/Shared/_Layout.cshtml`

### Staff sidebar (when `SessionHelper.IsStaff`)

| Sidebar item | Menu key checked | URL |
|--------------|------------------|-----|
| Dashboard | (always) | /Dashboard/Index |
| Dealerships | `admin_dealerships` | /Dealerships/Index |
| Vehicle Models | `admin_vehicle_models` | /VehicleModels/Index |
| Vehicle Colors | `admin_vehicle_colors` | /VehicleColors/Index |
| Price Management | `admin_prices` | /Prices/Index |
| Finance Names | `admin_finance_names` | /FinanceNames/Index |
| Document Types | `admin_document_types` | /DocumentTypes/Index |
| RTO Locations | `admin_rto_locations` | /RtoLocations/Index |
| Status Master | `admin_status_lookups` | /StatusLookups/Index |
| Subdealers | `admin_subdealers` | /Subdealers/Index |
| Balances | `admin_balances` | /Accounts/Index |
| Commission Rates | `admin_commission_rates` | /Commissions/Index |
| Manage Orders | `admin_orders` | /Orders/Index |
| Subdealer Vehicles | `admin_vehicles` | /Vehicles/Index |
| Vehicle Bookings | `admin_vehicle_bookings` | /VehicleBookings/Index |
| Return Requests | `admin_returns` | /Returns/Index |
| Payment Approvals | `admin_payments` | /Payments/Index |
| Reports | `admin_reports` OR `reports` | /Reports/Index |

### Subdealer sidebar (when `SessionHelper.IsSubdealer`)

| Sidebar item | Menu key | URL |
|--------------|----------|-----|
| Account Statement | `account_statements` | /Account/Statement |
| Create Order | `purchase_orders_create` | /Orders/Create |
| My Orders | `purchase_orders_view` | /Orders/MyOrders |
| My Vehicles | `vehicles_view` | /Vehicles/Index |
| Submit Commission | `commissions_submit` | /Commissions/Submit |
| My Payments | `my_payments` | /Payments/MyPayments |
| Reports | `reports` | /Reports/Index |

---

## 10. Controller Access Matrix

| Controller | Roles | Menu attribute | Notes |
|------------|-------|----------------|-------|
| AccountController | Public login; Statement=role 2 | `AccountStatements` | |
| DashboardController | 1,2,3,4 | — | All logged-in users |
| DealershipsController | 1 | `admin_dealerships` | System admin only |
| VehicleModelsController | 1 | — | |
| VehicleColorsController | 1 | — | |
| PricesController | 1 | — | |
| FinanceNamesController | 1 | `admin_finance_names` | |
| DocumentTypesController | 1 | `admin_document_types` | |
| RtoLocationsController | 1 | `admin_rto_locations` | |
| StatusLookupsController | 1 | `admin_status_lookups` | |
| SubdealersController | 1, 4 | `admin_subdealers` | |
| AccountsController | 1, 3 | — | Balances |
| CommissionsController | 1 (rates), 2 (submit) | `commissions_submit` | |
| OrdersController | 2 (create/view), 1,4 (manage) | `purchase_orders_*` | |
| VehiclesController | 1, 2, 4 | `vehicles_view` (subdealer) | |
| VehicleBookingsController | 1,4 (manage), 2 (book/deliver) | `vehicles_view` / staff menu | |
| ReturnsController | 1, 4 | — | |
| PaymentsController | 2 (submit), 1,3 (approve) | `my_payments` | |
| ReportsController | 1,2,3,4 | — | |

---

## 11. Step-by-Step: Add a New Screen

### Step 1 — Plan access
- Which roles? → `[AuthorizeRole(...)]`
- Which menu key? → new constant in `StaffMenuAccess` or `MenuKeys`
- Data scope? → subdealer-only or dealership-scoped

### Step 2 — Database (if new table/columns)
1. Create `ADD_MY_FEATURE.sql` in `KRSDealerManagement/`
2. Run on BOTH databases:
   ```powershell
   cd d:\KRS\Requirement
   .\run-db-changes.ps1 -Script "..\KRSDealerManagement\ADD_MY_FEATURE.sql"
   ```

### Step 3 — Domain entity
`Domain/Entities/MyEntity.cs`

### Step 4 — Repository
- If live DB column names differ → custom SQL (see `VehicleRepository.cs`)
- Register in `Infrastructure/Repositories/UnitOfWork.cs`:
  ```csharp
  public IRepository<MyEntity> MyEntities =>
      _myEntities ??= new MyEntityRepository(_context);
  ```
- Add property to `Domain/Repositories/IUnitOfWork.cs`

### Step 5 — Application layer
```
Application/Commands/CreateMyEntityCommand.cs
Application/Handlers/Commands/CreateMyEntityCommandHandler.cs
Application/Queries/GetMyEntitiesQuery.cs
Application/Handlers/Queries/GetMyEntitiesQueryHandler.cs
Application/DTOs/MyEntityDto.cs
Application/Validators/CreateMyEntityCommandValidator.cs   (optional)
```

Handler template: see `KRSDealerManagement/HANDLER_TEMPLATE.md`

### Step 6 — Controller
`Web/Controllers/MyEntitiesController.cs`
```csharp
[AuthorizeRole(1, 4)]
[AuthorizeMenu(StaffMenuAccess.MyFeature)]
public class MyEntitiesController : Controller
{
    private readonly IMediator _mediator;
    public MyEntitiesController(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> Index()
    {
        var list = await _mediator.Send(new GetMyEntitiesQuery());
        return View(list);
    }
}
```

### Step 7 — Views
```
Web/Views/MyEntities/Index.cshtml
Web/Views/MyEntities/Create.cshtml
```

### Step 8 — Sidebar link
Edit `Web/Views/Shared/_Layout.cshtml`:
```cshtml
@if (SessionHelper.HasMenuAccess(session, StaffMenuAccess.MyFeature))
{
    <li class="nav-item">
        <a href="@Url.Action("Index", "MyEntities")" class="nav-link">
            <i class="nav-icon bi bi-star"></i>
            <p>My Feature</p>
        </a>
    </li>
}
```

### Step 9 — Seed RoleMenus (production DB)
```sql
-- For each role that should see it:
INSERT INTO RoleMenus (RoleId, MenuKey, MenuName, IsAccessible, SortOrder)
SELECT RoleId, 'admin_my_feature', 'My Feature', 1, 99
FROM Roles WHERE RoleCode = 'SYSTEM_ADMIN';
```

### Step 10 — Build & test
```powershell
cd d:\KRS\KRSDealerManagement
dotnet build
dotnet run --project KRSDealerManagement.Web
```

---

## 12. Step-by-Step: Add a New Menu

### For staff menu

1. **Add constant** in `Shared/Constants/StaffMenuAccess.cs`:
   ```csharp
   public const string MyFeature = "admin_my_feature";
   ```

2. **Add to `AllAdminMenus()`** list with display name.

3. **Add to `CanAccess()`** switch for each role that should have it by default.

4. **Add sidebar link** in `_Layout.cshtml` (see Step 8 above).

5. **Add `[AuthorizeMenu(StaffMenuAccess.MyFeature)]`** on controller.

6. **Insert DB rows** in `RoleMenus` for each role (see SQL above).

7. **User must re-login** (or call `PermissionHelper.RefreshSessionAsync`) to see new menu.

### For subdealer menu

1. **Add constant** in `Shared/Constants/MenuKeys.cs`:
   ```csharp
   public const string MyFeature = "my_feature";
   ```

2. **Add to `GetSubdealerConfigurableMenus()`**:
   ```csharp
   (MyFeature, GetDisplayName(MyFeature), true),
   ```

3. **Add display name** in `GetDisplayName()` switch.

4. **Add sidebar link** in `_Layout.cshtml` subdealer section:
   ```cshtml
   @if (PermissionHelper.HasAccess(session, MenuKeys.MyFeature))
   ```

5. **Add `[AuthorizeMenu(MenuKeys.MyFeature)]`** on controller action.

6. **Ensure `RoleMenus`** has row for SUBDEALER role.

7. **Existing subdealers:** update via Subdealers → Details → Configure Permissions,  
   OR insert `AccountPermissions` rows for their account.

---

## 13. Step-by-Step: Change Who Can Access a Feature

### Change subdealer access (one subdealer)
1. Login as Admin or Branch Manager
2. Go to **Subdealers → Details** (select subdealer)
3. Check/uncheck menus → **Save Permissions**
4. Subdealer must **re-login** to see changes

### Change staff role access (all branch managers)
1. Update `RoleMenus` table in SQL:
   ```sql
   UPDATE RoleMenus SET IsAccessible = 0
   WHERE RoleId = (SELECT RoleId FROM Roles WHERE RoleCode = 'BRANCH_MANAGER')
     AND MenuKey = 'admin_returns';
   ```
2. Branch managers re-login

### Change controller-level role block
Edit `[AuthorizeRole(...)]` on the controller action in the `.cs` file. Rebuild and deploy.

### Give subdealer access to commission submit
1. Subdealers → Details → enable `commissions_submit`
2. Verify controller has:
   ```csharp
   [AuthorizeRole(2)]
   [AuthorizeMenu(MenuKeys.CommissionSubmit)]
   ```

---

## 14. Step-by-Step: Database Changes

### Rule: ALWAYS apply to BOTH databases

| Server | Connection |
|--------|------------|
| Production | `krsenterprise.in` / `KRSDealerManagementDB` (SQL auth: `krs`) |
| Local | `localhost\SQLEXPRESS` / `KRSDealerManagementDB` (Windows auth) |

Details: `Requirement/dbchanges`

### Run script on both (recommended)
```powershell
cd d:\KRS\Requirement
.\run-db-changes.ps1 -Script "..\KRSDealerManagement\APPLY_RECENT_DB_CHANGES.sql"
```

### Recent migration scripts

| Script | Purpose |
|--------|---------|
| `ADD_PRICE_EFFECTIVE_FROM.sql` | `EffectiveFrom` column on prices |
| `DROP_PRICE_MONTH_UNIQUE.sql` | Allow multiple prices per month |
| `ADD_VEHICLE_NOTES.sql` | `Notes` column on Vehicles |
| `ADD_VEHICLE_BOOKING.sql` | Booking tables |
| `APPLY_RECENT_DB_CHANGES.sql` | Combined recent changes |

### Manual sqlcmd
```powershell
# Production
sqlcmd -S krsenterprise.in -d KRSDealerManagementDB -U krs -P "..." -i script.sql -C

# Local
sqlcmd -S localhost\SQLEXPRESS -d KRSDealerManagementDB -i script.sql -C -E
```

---

## 15. Application Layer (Commands / Queries / Handlers)

### Folder structure
```
Application/
├── Commands/           ← write operations (CreateXxxCommand)
├── Queries/            ← read operations (GetXxxQuery)
├── Handlers/
│   ├── Commands/       ← CreateXxxCommandHandler
│   └── Queries/        ← GetXxxQueryHandler
├── DTOs/               ← data returned to views
├── Validators/         ← FluentValidation rules
├── Services/           ← shared services (VehiclePriceService, AuditService)
└── DependencyInjection.cs
```

### Adding a command (write)
1. Create `Commands/MyCommand.cs` implementing `IRequest<int>` (or return type)
2. Create `Handlers/Commands/MyCommandHandler.cs` implementing `IRequestHandler<MyCommand, int>`
3. Optionally create `Validators/MyCommandValidator.cs`
4. Call from controller: `await _mediator.Send(new MyCommand { ... })`

### Adding a query (read)
1. Create `Queries/GetMyQuery.cs` implementing `IRequest<List<MyDto>>`
2. Create `Handlers/Queries/GetMyQueryHandler.cs`
3. Call from controller: `var data = await _mediator.Send(new GetMyQuery())`

### Audit logging (always in handlers)
```csharp
await _auditService.LogActionAsync(
    entityType: "Vehicle",
    entityId: id,
    action: "Create",
    userId: request.CreatedBy,
    userRole: "Admin",
    newValue: JsonSerializer.Serialize(data)
);
```

### Account transaction logging
```csharp
await _auditService.LogTransactionAsync(
    accountId: accountId,
    transactionType: 1,        // 1=Debit, 2=Credit
    amount: amount,
    balanceAfter: balance.CurrentBalance,
    reason: "Price revision",
    referenceType: "Vehicle",
    referenceId: vehicleId,
    remarks: note,
    initiatedBy: userId
);
```

---

## 16. Repository Pattern & Live DB

### Generic repository (DANGEROUS for mismatched schemas)
**File:** `Infrastructure/Repositories/Repository.cs`  
Builds SQL from **all C# property names**. Fails if DB columns differ.

### Custom repositories (SAFE — use these as templates)

| Entity | Repository file | Why custom |
|--------|-----------------|------------|
| Vehicle | `VehicleRepository.cs` | `VehicleStatus` not `Status`; no `ManufacturingYear` on live |
| VehiclePriceHistory | `VehiclePriceHistoryRepository.cs` | `PriceMonth/PriceYear` not `Month/Year` |
| Commission | `CommissionRepository.cs` | `CommissionMonth`, `SubmittedAmount`, no `AccountId` |
| PurchaseOrder | `PurchaseOrderRepository.cs` | Live column name differences |
| SubdealerAccount | `SubdealerAccountRepository.cs` | Live column name differences |

### Live Vehicles table columns (what code uses)
```
VehicleId, ChassisNumber, ModelId, ColorId, VehicleStatus,
PurchaseOrderId, SubdealerId, CurrentPrice, OriginalPrice,
MotorNo, BatteryNo, ChargerNo, ControllerNo, ConverterNo,
Notes, CreatedDate, ModifiedDate
```

### When adding a new DB column
1. Write SQL script
2. Run on both DBs
3. Add property to Domain entity
4. **Update custom repository** SELECT and INSERT/UPDATE SQL
5. Do NOT rely on generic repository for that table

---

## 17. Key Business Features

### 17.1 Price Management

**Files:**
- `Application/Services/VehiclePriceService.cs`
- `Handlers/Commands/CreateVehiclePriceCommandHandler.cs`
- `Controllers/PricesController.cs`
- `Controllers/VehicleBookingsController.cs` (invoice trigger)

**Rules:**
| Event | What happens |
|-------|--------------|
| Admin saves new price with `EffectiveFrom` | Row in `VehiclePriceHistory`; revises already-invoiced vehicles where invoice/allocation date ≥ effective date |
| Staff sets `InvoiceDate` on booking | `ApplyPriceOnInvoiceAsync` — sets vehicle price to catalogue price as of invoice date; debits/credits subdealer account |
| Price lookup | `GetPriceAsOfAsync(model, color, date)` — latest price where `EffectiveFrom <= date` |

**Notes:** Appended to `Vehicles.Notes` on every price change. Visible in vehicle chassis modal.

### 17.2 Vehicle Booking Workflow

| Step | Who | Action |
|------|-----|--------|
| 1 | Subdealer | Book vehicle (chassis) |
| 2 | Staff | Process booking, set dates (invoice, insurance, etc.) |
| 3 | Subdealer | Upload subsidy documents |
| 4 | Staff | Apply subsidy ID |
| 5 | Subdealer only | Mark **Delivered** (only when status = Subsidy Applied) |

**Controller:** `VehicleBookingsController.cs`  
**Files upload path:** `Files/vehicle_booking/{yyyy_MM_dd}/{filename}`

### 17.3 Commission Submission

**Files:**
- `SubmitCommissionCommandHandler.cs`
- `CommissionsController.cs` (ValidateChassis AJAX + Submit)
- `Views/Commissions/Submit.cshtml`

**Rules:**
- Chassis must belong to logged-in subdealer
- Vehicle must be invoiced (`VehicleBookings.InvoiceDate` set)
- Amount must match commission rate for model/month/year
- No duplicate for same vehicle/month/year (non-rejected)

### 17.4 Purchase Orders

- Subdealer creates order → reserves balance
- Staff approves → allocates vehicles from PO items
- Staff can create order on behalf of subdealer

### 17.5 Excel Export & Pagination

- **Pagination:** `Web/Helpers/ListPagingHelper.cs` — default page size **10**
- **Excel:** `Web/Helpers/ExcelExportHelper.cs` + `Views/Shared/_ExportExcel.cshtml`
- Pattern: add `Export` action on Index controllers

---

## 18. Helpers & Utilities

| Helper | File | Purpose |
|--------|------|---------|
| `SessionHelper` | `Web/Helpers/SessionHelper.cs` | Session read/write, role checks |
| `PermissionHelper` | `Web/Helpers/PermissionHelper.cs` | Menu access, session refresh |
| `ListPagingHelper` | `Web/Helpers/ListPagingHelper.cs` | Pagination, date range filters |
| `ExcelExportHelper` | `Web/Helpers/ExcelExportHelper.cs` | Export list to .xlsx |
| `AccountHelper` | `Web/Helpers/AccountHelper.cs` | Get subdealer primary account |
| Query string crypto | `Web/Middleware/QueryStringEncryptionMiddleware.cs` | Encrypt URL parameters |
| `KrsQueryString` | `Web/wwwroot/js/query-string.js` | AJAX with encrypted params |
| `RedirectEncrypted` | Controller extension | Redirect with encrypted route values |

**Use encrypted URLs for detail pages:**
```csharp
return this.RedirectEncrypted(nameof(Manage), new { id });
```

**AJAX:**
```javascript
const resp = await KrsQueryString.fetchGet('/Commissions/ValidateChassis', { chassisNumber, modelId });
```

---

## 19. Deployment

### Build
```powershell
cd d:\KRS\KRSDealerManagement
dotnet publish KRSDealerManagement.Web -c Release -o ./publish
```

### Production (krsenterprise.in)
1. Run SQL scripts on production DB **first**
2. FTP publish output to `httpdocs`
3. Connection string: `appsettings.Production.json` — use `Server=localhost` when IIS and SQL on same server
4. `web.config` may set `ASPNETCORE_ENVIRONMENT=Production`

### Local dev
```powershell
dotnet run --project KRSDealerManagement.Web
```
Uses `appsettings.json` → `localhost\SQLEXPRESS`

---

## 20. Production Errors & Fixes

### Error: Invalid column name 'Status', 'Notes', etc. when creating price

**Cause:** Generic repository or entity properties don't match live `Vehicles` table.  
**Fix:**
1. Run `ADD_VEHICLE_NOTES.sql` on production
2. Ensure deployed code uses `VehicleRepository` (custom SQL)
3. Never use generic `Repository<Vehicle>` for reads/writes

### Error: Commission validation AccountId / VehicleId

**Cause:** Old code sent `VehicleId = 0`.  
**Fix:** Use `SubmitCommissionCommand` with `ChassisNumber` — handler resolves vehicle.

### Error: Connection to localhost on production

**Cause:** Wrong connection string in published `appsettings.json`.  
**Fix:** Use `appsettings.Production.json` with correct server name.

---

## 21. Test Logins

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | System Admin |
| `karur_mgr` | `KARUR@123` | Branch Manager (Karur) |
| (subdealer usernames) | `Subdealers@123` | Subdealer |

---

## 22. File Index (Where to Look)

| I want to… | Look here |
|------------|-----------|
| Change sidebar menu | `Web/Views/Shared/_Layout.cshtml` |
| Add staff menu key | `Shared/Constants/StaffMenuAccess.cs` |
| Add subdealer menu key | `Shared/Constants/MenuKeys.cs` |
| Change role access on action | `[AuthorizeRole]` on controller |
| Change menu access on action | `[AuthorizeMenu]` on controller |
| Change login / session | `AccountController.cs`, `LoginCommandHandler.cs`, `SessionHelper.cs` |
| Change subdealer permissions UI | `SubdealersController.cs`, `Views/Subdealers/Details.cshtml` |
| Change price logic | `VehiclePriceService.cs` |
| Change invoice → price | `VehicleBookingsController.Manage` POST |
| Change commission rules | `SubmitCommissionCommandHandler.cs` |
| Change SQL for live DB | `Infrastructure/Repositories/*Repository.cs` |
| Add DB column | `*.sql` script + run `run-db-changes.ps1` |
| Handler template | `HANDLER_TEMPLATE.md` |
| DB connections | `Requirement/dbchanges` |
| Run migrations | `Requirement/run-db-changes.ps1` |

---

## Quick Checklist: "Something doesn't show in menu"

- [ ] Is menu key in `RoleMenus` table for that role with `IsAccessible = 1`?
- [ ] For subdealer: is menu enabled in `AccountPermissions` for their account?
- [ ] Is sidebar `if` block present in `_Layout.cshtml`?
- [ ] Does controller have `[AuthorizeMenu(...)]` matching the key?
- [ ] Has user re-logged in after permission change?
- [ ] For system admin: `HasMenuAccess` always returns true — check sidebar `if` block exists

---

## Quick Checklist: "Access Denied"

- [ ] Check `[AuthorizeRole]` — is user's role ID in the list?
- [ ] Check `[AuthorizeMenu]` — is menu key in session? (re-login)
- [ ] Check data scope — branch manager trying to access another dealership's data?
- [ ] Subdealer trying staff-only action?

---

*End of Technical Guide*

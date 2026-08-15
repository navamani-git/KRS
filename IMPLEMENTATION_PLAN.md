# KRS Dealer Management System - Implementation Plan

**Status:** Planning Phase  
**Date:** August 7, 2026

---

## What I Will Do - Step by Step

### PHASE 1: Delete & Clean Start
- Delete all existing files from `d:\KRS\VehicleDealerMS` and `d:\KRS\KRSDealerManagement`
- Keep only:
  - `d:\KRS\Requirement\initialRequirement` (requirements)
  - `d:\KRS\Requirement\screens` (UI designs - if exists)
  - `d:\KRS\Specifications\` (business specs)
  - `d:\KRS\SOLUTION_ARCHITECTURE.md` (reference)

---

## PHASE 2: Single Solution with Class Libraries

### Project Structure (One Solution, Multiple Class Libraries)

```
d:\KRS\
│
└── KRSDealerManagement/ (Solution folder)
    │
    └── KRSDealerManagement.sln (Visual Studio Solution file)
    
    ├── 📦 KRSDealerManagement.Shared/ (Class Library)
    │   ├── Constants/
    │   │   ├── MenuKeys.cs
    │   │   ├── RoleConstants.cs
    │   │   └── StatusConstants.cs
    │   ├── Enums/
    │   │   ├── UserRoleEnum.cs
    │   │   ├── VehicleStatusEnum.cs
    │   │   ├── PurchaseOrderStatusEnum.cs
    │   │   └── CommissionStatusEnum.cs
    │   ├── Exceptions/
    │   │   ├── DomainException.cs
    │   │   ├── ValidationException.cs
    │   │   └── NotFoundException.cs
    │   ├── Results/
    │   │   ├── Result.cs (Success/Failure wrapper)
    │   │   └── PagedResult.cs
    │   └── Extensions/
    │       ├── StringExtensions.cs
    │       └── DateTimeExtensions.cs
    │
    ├── 📦 KRSDealerManagement.Domain/ (Class Library - Pure Business Logic)
    │   ├── Entities/
    │   │   ├── User.cs
    │   │   ├── Vehicle.cs
    │   │   ├── PurchaseOrder.cs
    │   │   ├── Commission.cs
    │   │   └── SubdealerAccount.cs
    │   ├── ValueObjects/
    │   │   ├── Money.cs
    │   │   ├── ChassisNumber.cs
    │   │   └── AccountPermission.cs
    │   ├── DomainServices/
    │   │   ├── IPriceCalculationService.cs
    │   │   ├── IBalanceValidationService.cs
    │   │   └── IPermissionService.cs
    │   ├── Specifications/
    │   │   ├── Specification.cs (base)
    │   │   ├── HasSufficientBalanceSpec.cs
    │   │   └── HasPermissionSpec.cs
    │   └── Interfaces/
    │       ├── IEntity.cs
    │       └── IRepository.cs (generic)
    │
    ├── 📦 KRSDealerManagement.Application/ (Class Library - CQRS)
    │   ├── Common/
    │   │   └── Interfaces/
    │   │       ├── IUnitOfWork.cs
    │   │       ├── ICurrentUserService.cs
    │   │       ├── IMediator.cs (or use MediatR NuGet)
    │   │       └── IMapper.cs
    │   ├── Commands/ (State-Changing Operations)
    │   │   ├── PurchaseOrders/
    │   │   │   ├── CreatePurchaseOrderCommand.cs
    │   │   │   ├── CreatePurchaseOrderCommandHandler.cs
    │   │   │   └── ApprovePurchaseOrderCommand.cs
    │   │   ├── Commissions/
    │   │   │   ├── SubmitCommissionCommand.cs
    │   │   │   └── ApproveCommissionCommand.cs
    │   │   ├── Accounts/
    │   │   │   ├── CreateSubdealerAccountCommand.cs
    │   │   │   └── ConfigurePermissionsCommand.cs
    │   │   └── Vehicles/
    │   │       └── CreateVehicleCommand.cs
    │   ├── Queries/ (Read-Only Operations)
    │   │   ├── PurchaseOrders/
    │   │   │   ├── GetPurchaseOrderQuery.cs
    │   │   │   └── GetPurchaseOrderQueryHandler.cs
    │   │   ├── Commissions/
    │   │   │   └── GetCommissionsQuery.cs
    │   │   ├── Accounts/
    │   │   │   ├── GetAccountsQuery.cs
    │   │   │   └── GetAccountPermissionsQuery.cs
    │   │   └── Vehicles/
    │   │       └── GetVehiclesQuery.cs
    │   ├── DTOs/ (Data Transfer Objects)
    │   │   ├── PurchaseOrderDto.cs
    │   │   ├── CommissionDto.cs
    │   │   ├── SubdealerAccountDto.cs
    │   │   ├── AccountPermissionDto.cs
    │   │   └── VehicleDto.cs
    │   ├── Services/ (Application Services)
    │   │   ├── AuthenticationService.cs
    │   │   ├── PurchaseOrderService.cs
    │   │   ├── CommissionService.cs
    │   │   └── AccountService.cs
    │   └── Mappings/
    │       └── MappingProfile.cs
    │
    ├── 📦 KRSDealerManagement.Infrastructure/ (Class Library - Data Access)
    │   ├── Persistence/
    │   │   ├── DatabaseContext.cs (Connection management)
    │   │   ├── Repositories/
    │   │   │   ├── Repository.cs (Generic base with Dapper)
    │   │   │   ├── UserRepository.cs
    │   │   │   ├── PurchaseOrderRepository.cs
    │   │   │   ├── CommissionRepository.cs
    │   │   │   ├── SubdealerAccountRepository.cs
    │   │   │   ├── VehicleRepository.cs
    │   │   │   └── IQueryRepository.cs (for queries)
    │   │   ├── UnitOfWork.cs (Transaction management)
    │   │   └── Migrations/
    │   │       └── InitialSchema.sql
    │   ├── Services/ (External dependencies)
    │   │   ├── DateTimeService.cs
    │   │   ├── CurrentUserService.cs
    │   │   └── EncryptionService.cs
    │   └── DependencyInjection.cs (Register services)
    │
    ├── 📦 KRSDealerManagement.Web/ (ASP.NET Core MVC)
    │   ├── Controllers/
    │   │   ├── BaseController.cs (Common logic)
    │   │   ├── AccountController.cs (Login/Register)
    │   │   ├── HomeController.cs
    │   │   ├── PurchaseOrderController.cs
    │   │   ├── CommissionController.cs
    │   │   ├── VehicleController.cs
    │   │   ├── AdminController.cs
    │   │   └── PermissionController.cs
    │   ├── Views/
    │   │   ├── Shared/
    │   │   │   ├── _Layout.cshtml
    │   │   │   ├── _Navigation.cshtml
    │   │   │   └── Error.cshtml
    │   │   ├── Home/
    │   │   ├── Account/
    │   │   ├── PurchaseOrder/
    │   │   ├── Commission/
    │   │   └── Admin/
    │   ├── ViewModels/
    │   │   └── *.cs (View-specific models)
    │   ├── Filters/
    │   │   └── AuthorizeByPermissionFilter.cs
    │   ├── Middleware/
    │   │   ├── ExceptionHandlingMiddleware.cs
    │   │   └── AuthenticationMiddleware.cs
    │   ├── Extensions/
    │   │   ├── ControllerExtensions.cs
    │   │   └── HttpContextExtensions.cs
    │   ├── wwwroot/
    │   │   ├── css/
    │   │   ├── js/
    │   │   └── images/
    │   ├── Program.cs (Main entry + DI setup)
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── KRSDealerManagement.Web.csproj
    │
    └── 📦 KRSDealerManagement.Tests/ (xUnit Test Project - Optional)
        ├── Domain/
        │   └── *.Tests.cs
        ├── Application/
        │   └── *.Tests.cs
        └── Infrastructure/
            └── *.Tests.cs
```

---

## PHASE 3: Project Dependencies (Layer Communication)

### Dependency Flow (Clean Architecture Rule)
```
Web (MVC) 
  ↓ depends on
Application (CQRS)
  ↓ depends on
Domain (Business Logic)
  
Infrastructure (Data Access)
  ↓ depends on
Domain (Interfaces only)

Shared (Constants/Enums)
  ↑ depended on by all layers
```

### What Each Layer Does

| Layer | Purpose | Contains | Depends On |
|-------|---------|----------|-----------|
| **Shared** | Common utilities | Constants, Enums, Exceptions, Utilities | Nothing |
| **Domain** | Pure business logic | Entities, Value Objects, Specifications, Domain Services | Shared only |
| **Application** | CQRS & orchestration | Commands, Queries, DTOs, Services | Domain, Shared |
| **Infrastructure** | Data access & external services | Repositories, Database context, External integrations | Domain (interfaces only), Shared |
| **Web** | User interface | Controllers, Views, Filters, Middleware | Application, Domain, Shared |

---

## PHASE 4: Technology Stack

### NuGet Packages to Use

**Web Project:**
- `Microsoft.AspNetCore.Mvc.Core` (included in ASP.NET Core)
- `Microsoft.AspNetCore.Authentication.Cookies`

**Application Project:**
- `MediatR` (CQRS implementation) - `dotnet add package MediatR`
- `AutoMapper` (DTO mapping) - `dotnet add package AutoMapper`
- `FluentValidation` (Validation) - `dotnet add package FluentValidation`

**Infrastructure Project:**
- `Dapper` (ORM) - `dotnet add package Dapper`
- `System.Data.SqlClient` (SQL Server provider)
- `Serilog` (Logging) - optional

**All Projects:**
- `.NET 8.0 SDK`

---

## PHASE 5: Key Design Patterns

### 1. **CQRS Pattern** (Commands & Queries)
- **Command:** Changes state (Create, Update, Delete)
  - Example: `CreatePurchaseOrderCommand` → Creates purchase order
  - Returns: `Result<int>` (order ID)
  
- **Query:** Reads data (no side effects)
  - Example: `GetPurchaseOrderQuery` → Fetches order details
  - Returns: `PurchaseOrderDto`

### 2. **Repository Pattern** (Data Access)
- Abstract database operations
- `IRepository<T>` interface with generic CRUD
- Example: `UserRepository.GetByIdAsync(userId)`

### 3. **Unit of Work Pattern** (Transaction Management)
- Manage multiple repositories in single transaction
- `IUnitOfWork` coordinates all repositories
- `SaveChangesAsync()` commits all changes together

### 4. **Dependency Injection** (DI Container)
- All dependencies injected via constructor
- Registered in `Program.cs`
- Makes testing easy

### 5. **Value Objects** (Domain Model)
- Immutable objects representing concepts
- Example: `Money`, `ChassisNumber`, `AccountPermission`
- Encapsulate validation logic

### 6. **Specifications Pattern** (Business Rules)
- Reusable business rules as classes
- Example: `HasSufficientBalanceSpec`
- Easy to test and reuse

---

## PHASE 6: Database Schema (SQL Server)

### Tables to Create
1. **Users** - Dealers and subdealers
2. **SubdealerAccounts** - Multiple accounts per subdealer
3. **AccountPermissions** - Menu access per account
4. **AccountBalance** - Balance per account (not per user)
5. **Vehicles** - Car inventory
6. **VehicleModels** - BMW, Toyota, etc.
7. **VehicleColors** - White, Black, etc.
8. **VehiclePriceHistory** - Monthly prices
9. **PurchaseOrders** - Orders from subdealers
10. **CommissionHistory** - Monthly commissions
11. **AccountTransactions** - Balance movements (audit trail)
12. **AuditLog** - All system changes

---

## PHASE 7: Features to Implement

### Admin Features
✓ Manage subdealers and their accounts  
✓ Configure menu permissions per account  
✓ Manage vehicle models and colors  
✓ Set monthly vehicle prices  
✓ Approve/Reject purchase orders  
✓ Approve/Reject commissions  
✓ View all transactions and audit logs  

### Subdealer Features
✓ Login with account selection  
✓ View multiple accounts  
✓ Switch between accounts  
✓ See different menus based on account permissions  
✓ Create purchase orders  
✓ Submit commission claims  
✓ View account balance and transactions  

### Multi-Account System
✓ Each subdealer can have N accounts  
✓ Each account has independent balance  
✓ Each account has independent transactions  
✓ Permissions configurable per account  
✓ Menu dynamically generated based on permissions  

---

## PHASE 8: Workflow Example

### Purchase Order Creation Workflow

```
1. User clicks "Create Purchase Order" on dashboard
   ↓
2. PurchaseOrderController.Create() (GET)
   ├── Load current account
   ├── Fetch available vehicles
   └── Return Create view
   ↓
3. User submits form
   ↓
4. PurchaseOrderController.Create() (POST)
   ├── Validate form
   ├── Call CreatePurchaseOrderCommand
   │
5. CreatePurchaseOrderCommandHandler.Handle()
   ├── Validate user has permission: "purchase_orders_create"
   ├── Get account from database
   ├── Check if account has sufficient balance
   ├── Calculate total price for vehicles
   ├── Create PurchaseOrder entity
   ├── Save via Repository
   ├── Commit via UnitOfWork
   ├── Return success with OrderId
   │
6. Controller receives result
   ├── If success: Redirect to success page
   ├── If error: Return view with error message
   │
7. System creates AccountTransaction record
   ├── Type: "PurchaseOrderCreated"
   ├── Amount reserved from balance
   ├── Timestamp and user recorded
```

---

## PHASE 9: Multi-Account Permission Example

### Scenario: Subdealer with 3 Accounts

**Account 1: "Main Sales"**
- Permission: Dashboard → ✓ Accessible
- Permission: Purchase Orders → ✓ Can Create, Edit
- Permission: Commissions → ✓ Can Submit, View
- Permission: Admin → ✗ No Access

**Account 2: "Fleet Operations"**
- Permission: Dashboard → ✓ Accessible
- Permission: Purchase Orders → ✓ View Only
- Permission: Commissions → ✗ No Access
- Permission: Vehicles → ✓ View Only

**Account 3: "Reports"**
- Permission: Dashboard → ✓ Accessible
- Permission: Account → ✓ View Only
- Permission: Reports → ✓ View Only
- Permission: Everything Else → ✗ No Access

### Menu Rendering
When subdealer logs in:
1. Select which account to use
2. System loads permissions for that account
3. Navigation menu built dynamically:
   - Only show accessible menus
   - Hide disabled menus
   - Disable create/edit buttons if no permission

---

## PHASE 10: Development Steps (In Order)

### Step 1: Create Solution Structure
- Create `.sln` file
- Create all 6 `.csproj` files (class libraries)

### Step 2: Build Shared Layer
- Constants, Enums, Exceptions, Results

### Step 3: Build Domain Layer
- Entities, Value Objects, Services, Specifications

### Step 4: Build Application Layer
- DTOs, Commands, Queries, Handlers, Services

### Step 5: Build Infrastructure Layer
- Repositories, Database Context, Unit of Work

### Step 6: Build Web (MVC)
- Controllers, Views, Filters, Middleware
- Register DI in Program.cs

### Step 7: Database
- Create SQL Server database
- Run schema script
- Seed initial data

### Step 8: Testing
- Write unit tests for critical paths
- Manual testing of user workflows

### Step 9: Deployment
- Build solution
- Publish to server
- Configure connection strings

---

## Summary of Approach

✅ **One Solution** - All projects in one `.sln` file  
✅ **Segregated Layers** - 6 separate class libraries  
✅ **Clean Architecture** - Dependency Inversion applied  
✅ **CQRS** - Separate read/write operations  
✅ **Dapper** - Lightweight ORM for data access  
✅ **MVC** - Web layer for UI  
✅ **Async/Await** - Modern async patterns  
✅ **Multi-Account** - Each subdealer has multiple accounts  
✅ **Configurable Permissions** - Admin manages menu access  
✅ **Enterprise-Ready** - Scalable, testable, maintainable  

---

## Next Steps

1. **Get Approval** - Confirm this plan is correct
2. **Delete Existing** - Clean all old files
3. **Create Solution** - Build project structure
4. **Implement Layers** - Start with Shared, then Domain, etc.
5. **Test & Deploy** - Verify and launch

---

**Status:** Plan Complete - Awaiting Approval  
**Created:** August 7, 2026

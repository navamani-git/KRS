# KRS Dealer Management - Modern Enterprise Architecture

**Version:** 2.0  
**Pattern:** Clean Architecture + CQRS + DDD  
**Technology:** .NET 8, Dapper, SQL Server

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
│  Web.MVC (ASP.NET Core MVC, Controllers, Views)             │
│  - HTTP Request/Response handling                           │
│  - User authentication and session management               │
│  - View rendering and model binding                         │
└─────────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  Application (CQRS Commands & Queries)                      │
│  - Commands: CreatePurchaseOrder, ApprovePurchase, etc.     │
│  - Queries: GetPurchaseOrders, GetAccountBalance, etc.      │
│  - DTOs: Data Transfer Objects (request/response)           │
│  - Handlers: Command/Query handlers with validation         │
│  - Mappers: DTO ↔ Domain Model conversions                  │
└─────────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────────┐
│                    DOMAIN LAYER (Business Logic)            │
│  Domain (Core Business Rules - DDD)                         │
│  - Domain Models/Entities                                   │
│  - Value Objects                                            │
│  - Aggregates (PurchaseOrder, Account, etc.)               │
│  - Domain Services                                          │
│  - Specifications (Business rules)                          │
│  - NO dependencies on external layers                       │
└─────────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                      │
│  Infrastructure (Data, External Services)                   │
│  - Repositories (Dapper ORM)                                │
│  - Unit of Work pattern                                     │
│  - Database context                                         │
│  - External service integrations                            │
│  - Email, SMS, Payment gateways                             │
└─────────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────────┐
│                   PERSISTENCE LAYER                         │
│  Database (SQL Server)                                      │
│  - Tables, Indexes, Constraints                             │
│  - Stored Procedures (optional)                             │
│  - Views (optional)                                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
Solution: KRSDealerManagement.sln
│
├── 📦 KRSDealerManagement.Shared/ (Class Library)
│   └── Shared utilities, constants, exceptions, enums
│
├── 📦 KRSDealerManagement.Domain/ (Class Library)
│   └── Domain Layer - Core business logic (NO dependencies)
│
├── 📦 KRSDealerManagement.Application/ (Class Library)
│   └── Application Layer - CQRS, DTOs, Handlers
│
├── 📦 KRSDealerManagement.Infrastructure/ (Class Library)
│   └── Infrastructure Layer - Repositories, Services
│
├── 📦 KRSDealerManagement.Web/ (ASP.NET Core MVC)
│   └── Presentation Layer - Controllers, Views, DI setup
│
└── 📦 KRSDealerManagement.Tests/ (xUnit)
    └── Unit tests for domain and application layers
```

---

## Layer Details

### 1. SHARED LAYER (Cross-cutting concerns)
```
KRSDealerManagement.Shared/
├── Constants/
│   ├── MenuKeys.cs
│   ├── RoleConstants.cs
│   ├── StatusConstants.cs
│   └── ErrorMessages.cs
├── Enums/
│   ├── UserRoleEnum.cs
│   ├── VehicleStatusEnum.cs
│   ├── PurchaseOrderStatusEnum.cs
│   └── CommissionStatusEnum.cs
├── Exceptions/
│   ├── DomainException.cs
│   ├── ValidationException.cs
│   ├── NotFoundException.cs
│   └── UnauthorizedAccessException.cs
├── Results/
│   ├── Result.cs (Success/Failure result wrapper)
│   └── PagedResult.cs (For paginated responses)
└── Extensions/
    ├── StringExtensions.cs
    ├── DateTimeExtensions.cs
    └── EnumExtensions.cs
```

### 2. DOMAIN LAYER (Pure business logic)
```
KRSDealerManagement.Domain/
├── Entities/
│   ├── User.cs (User entity - cannot change directly)
│   ├── Vehicle.cs
│   ├── PurchaseOrder.cs (Aggregate Root)
│   ├── Commission.cs
│   └── SubdealerAccount.cs (Aggregate Root)
├── ValueObjects/
│   ├── Money.cs (immutable value object)
│   ├── AccountPermission.cs
│   ├── OrderLineItem.cs
│   └── ChassisNumber.cs (strongly-typed string)
├── Aggregates/
│   ├── PurchaseOrderAggregate/
│   │   ├── PurchaseOrder.cs (root)
│   │   ├── OrderItem.cs
│   │   └── PurchaseOrderFactory.cs
│   └── SubdealerAccountAggregate/
│       ├── SubdealerAccount.cs (root)
│       ├── AccountPermission.cs
│       ├── AccountBalance.cs
│       └── AccountFactory.cs
├── DomainServices/
│   ├── IPriceCalculationService.cs
│   ├── IBalanceCalculationService.cs
│   ├── ICommissionCalculationService.cs
│   └── IPermissionValidationService.cs
├── Specifications/ (Business rules as classes)
│   ├── Specification.cs (base class)
│   ├── SubdealerHasSufficientBalanceSpec.cs
│   ├── AccountHasPermissionSpec.cs
│   ├── PurchaseOrderCanBeApprovedSpec.cs
│   └── CommissionCanBeSubmittedSpec.cs
├── Events/ (Domain events)
│   ├── DomainEvent.cs (base class)
│   ├── PurchaseOrderCreatedEvent.cs
│   ├── CommissionApprovedEvent.cs
│   ├── BalanceUpdatedEvent.cs
│   └── PermissionChangedEvent.cs
├── Interfaces/ (Contracts - not implementations)
│   ├── IEntity.cs
│   ├── IAggregateRoot.cs
│   ├── IDomainEventPublisher.cs
│   └── IRepository.cs (generic)
└── Exceptions/
    ├── InsufficientBalanceException.cs
    ├── UnauthorizedAccountAccessException.cs
    ├── InvalidPurchaseOrderException.cs
    └── InvalidCommissionException.cs
```

### 3. APPLICATION LAYER (CQRS)
```
KRSDealerManagement.Application/
├── Common/
│   ├── Interfaces/
│   │   ├── IUnitOfWork.cs
│   │   ├── ICurrentUserService.cs
│   │   ├── IDateTime.cs
│   │   └── IMapper.cs
│   ├── Mappings/
│   │   ├── MappingProfile.cs (AutoMapper)
│   │   └── CustomMappers/
│   └── Behaviours/ (CQRS pipeline)
│       ├── ValidationBehaviour.cs
│       ├── LoggingBehaviour.cs
│       ├── UnhandledExceptionBehaviour.cs
│       └── PerformanceBehaviour.cs
│
├── CQRS/
│   ├── Commands/
│   │   ├── PurchaseOrders/
│   │   │   ├── CreatePurchaseOrderCommand.cs
│   │   │   ├── CreatePurchaseOrderCommandHandler.cs
│   │   │   ├── CreatePurchaseOrderCommandValidator.cs
│   │   │   └── ApprovePurchaseOrderCommand.cs
│   │   ├── Commissions/
│   │   │   ├── SubmitCommissionCommand.cs
│   │   │   └── ApproveCommissionCommand.cs
│   │   ├── Accounts/
│   │   │   ├── CreateSubdealerAccountCommand.cs
│   │   │   ├── ConfigureAccountPermissionsCommand.cs
│   │   │   └── SetAccountBalanceCommand.cs
│   │   └── Vehicles/
│   │       ├── CreateVehicleCommand.cs
│   │       └── UpdateVehicleStatusCommand.cs
│   │
│   └── Queries/
│       ├── PurchaseOrders/
│       │   ├── GetPurchaseOrderQuery.cs
│       │   ├── GetPurchaseOrderQueryHandler.cs
│       │   ├── GetPurchaseOrdersQuery.cs
│       │   └── GetPurchaseOrdersQueryHandler.cs
│       ├── Commissions/
│       │   ├── GetCommissionsQuery.cs
│       │   └── GetCommissionDetailsQuery.cs
│       ├── Accounts/
│       │   ├── GetSubdealerAccountsQuery.cs
│       │   ├── GetAccountBalanceQuery.cs
│       │   └── GetAccountPermissionsQuery.cs
│       └── Vehicles/
│           ├── GetVehiclesQuery.cs
│           └── GetVehicleDetailsQuery.cs
│
├── DTOs/
│   ├── PurchaseOrderDto.cs
│   ├── CommissionDto.cs
│   ├── SubdealerAccountDto.cs
│   ├── AccountPermissionDto.cs
│   ├── AccountBalanceDto.cs
│   ├── VehicleDto.cs
│   └── Requests/
│       ├── CreatePurchaseOrderRequest.cs
│       ├── ApproveCommissionRequest.cs
│       └── ConfigurePermissionsRequest.cs
│
├── Services/ (Application Services)
│   ├── AuthenticationService.cs
│   ├── AccountService.cs
│   ├── PurchaseOrderService.cs
│   ├── CommissionService.cs
│   └── PermissionService.cs
│
└── Exceptions/
    ├── ApplicationException.cs
    └── ValidationException.cs
```

### 4. INFRASTRUCTURE LAYER (Data access)
```
KRSDealerManagement.Infrastructure/
├── Persistence/
│   ├── DatabaseContext.cs (Connection management)
│   ├── Repositories/
│   │   ├── Repository.cs (Generic base)
│   │   ├── UserRepository.cs
│   │   ├── PurchaseOrderRepository.cs
│   │   ├── CommissionRepository.cs
│   │   ├── SubdealerAccountRepository.cs
│   │   ├── VehicleRepository.cs
│   │   └── AccountPermissionRepository.cs
│   ├── UnitOfWork.cs (Transactional operations)
│   └── Migrations/ (SQL scripts)
│       ├── 001_InitialSchema.sql
│       ├── 002_AddMultiAccountSupport.sql
│       └── 003_AddAuditTables.sql
│
├── Services/ (External integrations)
│   ├── DateTimeService.cs
│   ├── CurrentUserService.cs
│   ├── EmailService.cs (if needed)
│   ├── SmsService.cs (if needed)
│   └── PaymentGatewayService.cs (if needed)
│
├── Configuration/ (Dapper setup)
│   ├── DapperConfiguration.cs
│   └── TypeHandlers/ (Custom type mappings)
│
└── DependencyInjection.cs (Extension method for DI)
```

### 5. PRESENTATION LAYER (MVC)
```
KRSDealerManagement.Web/
├── Controllers/
│   ├── BaseController.cs (Common logic)
│   ├── HomeController.cs
│   ├── AccountController.cs (Login/Logout)
│   ├── DealerController.cs (Admin features)
│   ├── SubdealerController.cs (Subdealer features)
│   ├── PurchaseOrderController.cs
│   ├── CommissionController.cs
│   ├── VehicleController.cs
│   └── PermissionController.cs (Admin)
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _Navigation.cshtml
│   │   ├── _ValidationSummary.cshtml
│   │   └── _MenuPermissions.cshtml
│   ├── Home/
│   │   ├── Index.cshtml
│   │   └── Dashboard.cshtml
│   ├── Account/
│   │   ├── Login.cshtml
│   │   └── SelectAccount.cshtml
│   ├── PurchaseOrder/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   └── Details.cshtml
│   ├── Commission/
│   │   ├── Index.cshtml
│   │   ├── Submit.cshtml
│   │   └── History.cshtml
│   ├── Admin/
│   │   ├── SubdealerAccounts.cshtml
│   │   ├── ConfigurePermissions.cshtml
│   │   └── Dashboard.cshtml
│   └── Error/
│       ├── NotFound.cshtml
│       ├── Unauthorized.cshtml
│       └── Error.cshtml
│
├── ViewModels/ (View-specific models)
│   ├── PurchaseOrderViewModel.cs
│   ├── CommissionViewModel.cs
│   ├── AccountViewModel.cs
│   └── PermissionConfigViewModel.cs
│
├── Filters/ (Action filters)
│   ├── AuthorizeByAccountPermissionFilter.cs
│   ├── ValidateModelFilter.cs
│   └── ExceptionFilter.cs
│
├── Extensions/ (Helper extensions)
│   ├── ControllerExtensions.cs
│   ├── HttpContextExtensions.cs
│   └── UserExtensions.cs
│
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   ├── AuthenticationMiddleware.cs
│   └── PermissionMiddleware.cs
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css
│   │   └── bootstrap-custom.css
│   ├── js/
│   │   ├── site.js
│   │   └── modals.js
│   └── images/
│
├── Program.cs (Main entry point & DI setup)
├── Startup configuration files
└── appsettings.json
```

---

## Key Modern Patterns Used

### 1. Clean Architecture
- **Dependency Rule:** Inner layers don't depend on outer layers
- **Domain at center:** Business logic independent of frameworks
- **Testable:** Each layer can be tested independently

### 2. CQRS (Command Query Responsibility Segregation)
- **Commands:** Modify state (Create, Update, Delete)
- **Queries:** Read data (no side effects)
- **Separation:** Clearer intent and easier to optimize

### 3. Repository Pattern
- **Abstraction:** Data access hidden behind interfaces
- **Testability:** Easy to mock in tests
- **Consistency:** Single place to manage data operations

### 4. Dependency Injection
- **Loose Coupling:** Components don't create dependencies
- **Configuration:** DI setup in one place (Program.cs)
- **Testing:** Easy to inject mocks

### 5. Value Objects (DDD)
- **Immutability:** Cannot be changed after creation
- **Type Safety:** `Money`, `ChassisNumber`, `OrderLineItem`
- **Business Rules:** Encapsulate validation logic

### 6. Aggregates (DDD)
- **Consistency:** All related entities updated together
- **Boundaries:** `PurchaseOrderAggregate`, `SubdealerAccountAggregate`
- **Transactions:** Atomic operations within aggregate

### 7. Specifications Pattern
- **Reusability:** Business rules as classes
- **Testability:** Easy to test specifications
- **Clarity:** Rules explicit in code

### 8. Domain Events
- **Eventual Consistency:** Events trigger side effects
- **Decoupling:** Components don't directly call each other
- **Audit Trail:** All important events logged

### 9. Unit of Work Pattern
- **Transaction Management:** Multiple repos in single transaction
- **Consistency:** All changes committed together or rolled back

### 10. Async/Await
- **Performance:** Non-blocking operations
- **Scalability:** Handle more concurrent requests
- **Modern:** Built-in async support

---

## Data Flow Example: Create Purchase Order

```
User submits form in View
        ↓
PurchaseOrderController.Create (POST)
        ↓
CreatePurchaseOrderCommand (CQRS)
        ↓
CreatePurchaseOrderCommandValidator (Validation)
        ↓
CreatePurchaseOrderCommandHandler
        ├── CurrentUserService.GetCurrentUser()
        ├── UnitOfWork.SubdealerAccountRepository.GetAsync(accountId)
        ├── DomainService.ValidateBalance(account, totalAmount)
        ├── PurchaseOrderAggregate.Create(items, accountId)
        ├── SubdealerHasSufficientBalanceSpec.IsSatisfiedBy(account)
        ├── UnitOfWork.PurchaseOrderRepository.AddAsync(order)
        ├── UnitOfWork.SaveChangesAsync()
        ├── PublishDomainEvent(PurchaseOrderCreatedEvent)
        └── Return Result<int> (OrderId)
        ↓
Mapper converts domain model to DTO
        ↓
Response sent to View
        ↓
View displays success message
```

---

## Data Flow Diagram: Account Permission Check

```
Request enters with User + Account
        ↓
PermissionMiddleware
        ↓
Get CurrentAccount from session
        ↓
Query: GetAccountPermissionsQuery
        ↓
Query loads permissions from repository
        ↓
PermissionService checks access
        ↓
[HasPermission?]
├─ YES → Continue to controller
└─ NO → Throw UnauthorizedAccessException
        ↓
ExceptionFilter catches exception
        ↓
Return HTTP 403 Forbidden
```

---

## Benefits of This Architecture

✅ **Scalable** - Easy to add new features  
✅ **Testable** - Each layer independently testable  
✅ **Maintainable** - Clear separation of concerns  
✅ **Flexible** - Easy to swap implementations  
✅ **Modern** - Follows industry best practices  
✅ **Enterprise-Ready** - Used by major corporations  
✅ **Microservices-Ready** - Can split into services later  
✅ **Type-Safe** - Strong typing throughout  
✅ **Async** - High performance and scalability  
✅ **Business-Focused** - Domain logic at center  

---

## Getting Started

1. Create all class library projects
2. Set up dependency injection in Program.cs
3. Implement Domain models and Aggregates
4. Create Application layer CQRS commands/queries
5. Implement Infrastructure repositories
6. Build MVC controllers and views
7. Write tests for critical paths
8. Deploy and monitor

---

**Status:** Architecture designed  
**Next Steps:** Implement projects according to this structure

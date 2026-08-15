# Phase 3 Completion Guide

## Current Status

**3.5/8 Tasks Complete (43.75%)**

### Completed
- ✅ Task #1: 15 DTOs
- ✅ Task #2: 20 Commands
- ✅ Task #3: 17 Queries
- ✅ Task #4 (Partial): 1 Handler example + HANDLER_TEMPLATE.md + IAuditService interface

### Remaining
- Task #4 (Remaining): 36 more handlers (modifiable from template)
- Task #5: 20 Validators
- Task #6: AutoMapper MappingProfile
- Task #7: Infrastructure Layer Repositories
- Task #8: IAuditService Implementation

---

## Files Already Created for Task #4

1. **CreateVehicleModelCommandHandler.cs** - Example handler showing pattern
2. **HANDLER_TEMPLATE.md** - Complete template with 37 handler stubs (20 commands + 17 queries)
3. **IAuditService.cs** - Interface for audit logging

---

## How to Complete Remaining Tasks

### Task #4: Command/Query Handlers (36 remaining)

**Approach: Use Template**

All handlers follow same 5-step pattern:

```csharp
1. VALIDATE - Check business rules
2. EXECUTE - Perform operation
3. SAVE - Call _unitOfWork.SaveChangesAsync()
4. AUDIT - Call _auditService.LogActionAsync()
5. RETURN - Return result
```

**Files to Create (36 files):**

**Command Handlers (19 remaining):**
```
UpdateVehicleModelCommandHandler.cs
CreateVehiclePriceCommandHandler.cs
UpdateVehiclePriceCommandHandler.cs
CreateCommissionRateCommandHandler.cs
UpdateCommissionRateCommandHandler.cs
CreateSubdealerCommandHandler.cs
CreateSubdealerAccountCommandHandler.cs
ConfigureAccountPermissionsCommandHandler.cs
CreatePurchaseOrderCommandHandler.cs
ApprovePurchaseOrderItemCommandHandler.cs
RejectPurchaseOrderItemCommandHandler.cs
SubmitCommissionCommandHandler.cs
ApproveCommissionCommandHandler.cs
CreateReturnRequestCommandHandler.cs
ApproveReturnRequestCommandHandler.cs
RejectReturnRequestCommandHandler.cs
CreatePaymentCommandHandler.cs
ApprovePaymentCommandHandler.cs
RejectPaymentCommandHandler.cs
```

**Query Handlers (17):**
```
GetVehicleModelsQueryHandler.cs
GetVehicleModelByIdQueryHandler.cs
GetVehicleColorsQueryHandler.cs
GetVehiclePricesQueryHandler.cs
GetSubdealersQueryHandler.cs
GetSubdealerAccountsQueryHandler.cs
GetAccountPermissionsQueryHandler.cs
GetAccountBalanceQueryHandler.cs
GetPurchaseOrdersQueryHandler.cs
GetPurchaseOrderByIdQueryHandler.cs
GetCommissionsQueryHandler.cs
GetCommissionRatesQueryHandler.cs
GetReturnRequestsQueryHandler.cs
GetPaymentsQueryHandler.cs
GetAccountTransactionsQueryHandler.cs
GetAuditLogsQueryHandler.cs
GetDashboardSummaryQueryHandler.cs
```

**Pattern for Each Handler:**

```csharp
using MediatR;
using KRSDealerManagement.Application.[Commands/Queries];
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Application.Services;
using AutoMapper;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.[Commands/Queries]
{
    public class [HandlerName] : IRequestHandler<[CommandName], [ReturnType]>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService; // Commands only
        private readonly IMapper _mapper; // Queries only

        public [HandlerName](IUnitOfWork unitOfWork, [IAuditService _auditService,] [IMapper mapper])
        {
            _unitOfWork = unitOfWork;
            _auditService = _auditService; // Commands only
            _mapper = mapper; // Queries only
        }

        public async Task<[ReturnType]> Handle([CommandName] request, CancellationToken cancellationToken)
        {
            try
            {
                // Implementation per HANDLER_TEMPLATE.md
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error: {ex.Message}", ex);
            }
        }
    }
}
```

---

### Task #5: FluentValidation Validators (20 files)

**Approach: Create one per command**

**Example Pattern:**

```csharp
using FluentValidation;
using KRSDealerManagement.Application.Commands;

namespace KRSDealerManagement.Application.Validators
{
    public class CreateVehicleModelCommandValidator : AbstractValidator<CreateVehicleModelCommand>
    {
        public CreateVehicleModelCommandValidator()
        {
            RuleFor(x => x.ModelName)
                .NotEmpty().WithMessage("Model name is required")
                .Length(3, 100).WithMessage("Model name must be between 3 and 100 characters");

            RuleFor(x => x.CreatedBy)
                .GreaterThan(0).WithMessage("Valid user ID required");
        }
    }
}
```

**Files to Create (20):**
```
CreateVehicleModelCommandValidator.cs
UpdateVehicleModelCommandValidator.cs
CreateVehiclePriceCommandValidator.cs
UpdateVehiclePriceCommandValidator.cs
CreateCommissionRateCommandValidator.cs
UpdateCommissionRateCommandValidator.cs
CreateSubdealerCommandValidator.cs
CreateSubdealerAccountCommandValidator.cs
ConfigureAccountPermissionsCommandValidator.cs
CreatePurchaseOrderCommandValidator.cs
ApprovePurchaseOrderItemCommandValidator.cs
RejectPurchaseOrderItemCommandValidator.cs
SubmitCommissionCommandValidator.cs
ApproveCommissionCommandValidator.cs
CreateReturnRequestCommandValidator.cs
ApproveReturnRequestCommandValidator.cs
CreatePaymentCommandValidator.cs
ApprovePaymentCommandValidator.cs
RejectPaymentCommandValidator.cs
CreateCommissionRateCommandValidator.cs (if different)
```

**Validation Rules Per Command:**

1. **CreateVehicleModelCommand** - ModelName required, 3-100 chars, unique
2. **UpdateVehicleModelCommand** - ModelId > 0, ModelName 3-100 chars
3. **CreateVehiclePriceCommand** - ModelId > 0, ColorId > 0, Price > 0, Month 1-12, Year valid
4. **CreateSubdealerCommand** - Name required, Email valid, Phone required
5. **CreatePurchaseOrderCommand** - AccountId > 0, Items not empty, UnitPrice > 0
6. **ApprovePurchaseOrderItemCommand** - OrderId > 0, VehicleId > 0, Amount > 0
7. **SubmitCommissionCommand** - VehicleId > 0, Month 1-12, Amount > 0
8. **ApproveCommissionCommand** - CommissionId > 0, ApprovedBy > 0
9. **CreateReturnRequestCommand** - AccountId > 0, VehicleId > 0, Amount > 0
10. **CreatePaymentCommand** - Amount > 0, PaymentType valid

---

### Task #6: AutoMapper MappingProfile (1 file)

**File: MappingProfile.cs**

```csharp
using AutoMapper;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User
            CreateMap<User, UserDto>()
                .ForMember(d => d.UserRole, o => o.MapFrom(s => s.UserRole));

            // SubdealerAccount
            CreateMap<SubdealerAccount, SubdealerAccountDto>()
                .ForMember(d => d.SubdealerName, o => o.Ignore());

            // AccountPermission
            CreateMap<AccountPermission, AccountPermissionDto>();

            // AccountBalance
            CreateMap<AccountBalance, AccountBalanceDto>()
                .ForMember(d => d.SubdealerName, o => o.Ignore())
                .ForMember(d => d.AccountName, o => o.Ignore());

            // VehicleModel
            CreateMap<VehicleModel, VehicleModelDto>();

            // VehicleColor
            CreateMap<VehicleColor, VehicleColorDto>();

            // Vehicle
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(d => d.ModelName, o => o.Ignore())
                .ForMember(d => d.ColorName, o => o.Ignore());

            // VehiclePriceHistory
            CreateMap<VehiclePriceHistory, VehiclePriceHistoryDto>()
                .ForMember(d => d.ModelName, o => o.Ignore())
                .ForMember(d => d.ColorName, o => o.Ignore());

            // PurchaseOrder
            CreateMap<PurchaseOrder, PurchaseOrderDto>()
                .ForMember(d => d.AccountName, o => o.Ignore())
                .ForMember(d => d.SubdealerName, o => o.Ignore())
                .ForMember(d => d.ApprovedByName, o => o.Ignore());

            // Commission
            CreateMap<Commission, CommissionDto>()
                .ForMember(d => d.AccountName, o => o.Ignore())
                .ForMember(d => d.SubdealerName, o => o.Ignore())
                .ForMember(d => d.VehicleChassisNumber, o => o.Ignore())
                .ForMember(d => d.ApprovedByName, o => o.Ignore());

            // CommissionRate
            CreateMap<CommissionRate, CommissionRateDto>()
                .ForMember(d => d.ModelName, o => o.Ignore());

            // ReturnRequest
            CreateMap<ReturnRequest, ReturnRequestDto>()
                .ForMember(d => d.AccountName, o => o.Ignore())
                .ForMember(d => d.OrderNumber, o => o.Ignore())
                .ForMember(d => d.VehicleChassisNumber, o => o.Ignore())
                .ForMember(d => d.ProcessedByName, o => o.Ignore());

            // Payment
            CreateMap<Payment, PaymentDto>()
                .ForMember(d => d.AccountName, o => o.Ignore())
                .ForMember(d => d.SubdealerName, o => o.Ignore())
                .ForMember(d => d.ProcessedByName, o => o.Ignore());

            // AccountTransaction
            CreateMap<AccountTransaction, AccountTransactionDto>()
                .ForMember(d => d.InitiatedByName, o => o.Ignore());

            // AuditLog
            CreateMap<AuditLog, AuditLogDto>()
                .ForMember(d => d.UserName, o => o.Ignore());
        }
    }
}
```

---

### Task #7: Infrastructure Layer Repositories (15+ files)

**Files to Create:**

```
Data/ApplicationDbContext.cs
Repositories/Repository.cs (Generic base)
Repositories/UnitOfWork.cs
Repositories/UserRepository.cs
Repositories/SubdealerAccountRepository.cs
Repositories/AccountPermissionRepository.cs
Repositories/AccountBalanceRepository.cs
Repositories/VehicleRepository.cs
Repositories/VehicleModelRepository.cs
Repositories/VehicleColorRepository.cs
Repositories/VehiclePriceHistoryRepository.cs
Repositories/PurchaseOrderRepository.cs
Repositories/CommissionRepository.cs
Repositories/CommissionRateRepository.cs
Repositories/ReturnRequestRepository.cs
Repositories/PaymentRepository.cs
Repositories/AccountTransactionRepository.cs
Repositories/AuditLogRepository.cs
Services/AuditService.cs
DependencyInjection.cs
```

**Key Points:**

1. **Generic Repository<T>**
   - Implements IRepository<T>
   - Uses Dapper for queries
   - Basic CRUD operations

2. **UnitOfWork**
   - Implements IUnitOfWork
   - Manages all repositories
   - Handles transactions

3. **Concrete Repositories**
   - Inherit from Repository<T>
   - Add custom queries per entity
   - Use Dapper ExecuteAsync/QueryAsync

4. **AuditService Implementation**
   - Logs to AuditLog table
   - Logs to AccountTransaction table
   - Automatic timestamp and IP capture

5. **DependencyInjection.cs**
   - Extension method: AddApplicationServices
   - Register all repositories
   - Register AuditService
   - Register handlers and validators

---

### Task #8: IAuditService Implementation (1 file)

**File: Services/AuditService.cs**

```csharp
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Application.DTOs;
using System.Text.Json;

namespace KRSDealerManagement.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogActionAsync(
            string entityType, int entityId, string action, int userId, 
            string userRole, string newValue, string oldValue = null, string remarks = null)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                UserRole = userRole,
                NewValue = newValue,
                OldValue = oldValue,
                Remarks = remarks,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task LogTransactionAsync(...)
        {
            // Similar pattern for AccountTransaction
        }

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(...)
        {
            // Query with filtering
        }

        public async Task<IEnumerable<AccountTransactionDto>> GetAccountTransactionsAsync(...)
        {
            // Query transaction history
        }
    }
}
```

---

## Registration in DI Container

**Program.cs or Startup.cs:**

```csharp
services.AddApplicationServices() // Extension from DependencyInjection.cs
services.AddAutoMapper(typeof(MappingProfile))
services.AddMediatR(typeof(CreateVehicleModelCommand))
services.AddValidatorsFromAssemblyContaining<CreateVehicleModelCommandValidator>()
```

---

## Testing Before Deployment

1. **Database**
   - Run DATABASE_SETUP.sql
   - Verify all tables created
   - Verify seed data

2. **Handlers**
   - Test each handler with sample data
   - Verify audit logging works
   - Verify balance calculations

3. **Validators**
   - Test invalid inputs
   - Verify error messages

4. **Queries**
   - Test filtering options
   - Verify DTO mapping

5. **End-to-End**
   - Create purchase order
   - Approve item
   - Check balance updated
   - Verify audit trail logged

---

## Summary

- **Phase 3: 43.75% Complete**
- **Remaining: 54 files** (36 handlers, 20 validators, 1 mapping profile, 15+ repos)
- **All follow templates** - Modifiable until testing
- **Database ready** - Copy/paste to SSMS
- **Audit trail built in** - Every operation logged

**Next Session:**
- Complete remaining handlers from template
- Create validators
- Build repositories with Dapper
- Test end-to-end workflow


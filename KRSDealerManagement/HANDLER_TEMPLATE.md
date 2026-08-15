# Handler Implementation Template

## Generic Command Handler Template

```csharp
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class [CommandName]Handler : IRequestHandler<[CommandName], [ReturnType]>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public [CommandName]Handler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<[ReturnType]> Handle([CommandName] request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. VALIDATE
                // Check business rules using domain services

                // 2. EXECUTE
                // Perform operation (Create, Update, Delete)
                // Call repositories via _unitOfWork

                // 3. SAVE
                await _unitOfWork.SaveChangesAsync();

                // 4. AUDIT
                await _auditService.LogActionAsync(
                    entityType: "[Entity]",
                    entityId: [id],
                    action: "[Action]",
                    userId: request.CreatedBy,
                    userRole: "[Role]",
                    newValue: JsonSerializer.Serialize([newValue])
                );

                // 5. RETURN
                return [result];
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error: {ex.Message}", ex);
            }
        }
    }
}
```

## Generic Query Handler Template

```csharp
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;
using AutoMapper;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class [QueryName]Handler : IRequestHandler<[QueryName], [ReturnType]>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public [QueryName]Handler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<[ReturnType]> Handle([QueryName] request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. QUERY
                // Get data from repositories

                // 2. FILTER
                // Apply filtering based on request parameters

                // 3. MAP
                // Map entities to DTOs using AutoMapper
                var result = _mapper.Map<[ReturnType]>([entity]);

                // 4. RETURN
                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error querying data: {ex.Message}", ex);
            }
        }
    }
}
```

---

## Command Handlers Needed (20 total)

### Vehicle Management (4)
1. **CreateVehicleModelCommandHandler** ✅ (Created)
   - Create VehicleModel entity
   - Log to AuditLog
   - Return ModelId

2. **UpdateVehicleModelCommandHandler**
   - Fetch existing model
   - Update properties
   - Log changes (OldValue → NewValue)
   - Return success

3. **CreateVehiclePriceCommandHandler**
   - Create VehiclePriceHistory
   - Handle multiple prices per model/color/month
   - Log to AuditLog
   - Return PriceHistoryId

4. **UpdateVehiclePriceCommandHandler**
   - Update price with reason
   - Log old/new prices
   - Return success

### Commission Management (3)
5. **CreateCommissionRateCommandHandler**
   - Create CommissionRate
   - Set effective dates
   - Log to AuditLog
   - Return CommissionRateId

6. **UpdateCommissionRateCommandHandler**
   - Update rate and expiry dates
   - Log changes
   - Return success

7. **SubmitCommissionCommandHandler**
   - Create Commission entity
   - Validate one per vehicle per month
   - Log to AuditLog
   - Return CommissionId

### Subdealer Management (3)
8. **CreateSubdealerCommandHandler**
   - Create User (Subdealer role)
   - Create SubdealerAccount (Main)
   - Create AccountBalance (with initial amount)
   - Create AccountPermissions (default)
   - Generate username/password
   - Return UserId

9. **CreateSubdealerAccountCommandHandler**
   - Create new SubdealerAccount
   - Create AccountBalance
   - Create default permissions
   - Return AccountId

10. **ConfigureAccountPermissionsCommandHandler**
    - Update AccountPermission records
    - Log each change
    - Return success

### Purchase Order Management (4)
11. **CreatePurchaseOrderCommandHandler**
    - Validate account has sufficient balance
    - Create PurchaseOrder
    - Create Vehicle records for each item
    - Reserve amount from balance
    - Create AccountTransaction (Reserved)
    - Log to AuditLog
    - Return OrderId

12. **ApprovePurchaseOrderItemCommandHandler**
    - Update order item status to Approved
    - Debit amount from account balance
    - Create AccountTransaction (Debit)
    - Release reserved amount
    - Log approval with remarks
    - Return success

13. **RejectPurchaseOrderItemCommandHandler**
    - Update order item status to Rejected
    - Release reserved amount
    - Create AccountTransaction (Release)
    - Log rejection with remarks
    - Return success

14. **CreateReturnRequestCommandHandler**
    - Create ReturnRequest
    - Validate vehicle belongs to order
    - Hold refund pending approval
    - Log to AuditLog
    - Return ReturnRequestId

### Commission Approval (2)
15. **ApproveCommissionCommandHandler**
    - Update Commission status to Approved
    - Get CommissionRate for validation
    - Log approval
    - Mark for payment
    - Return success

16. **SubmitCommissionCommandHandler** (Alternative to #7)
    - Same as CreateCommissionCommandHandler
    - For subdealer submission

### Return Management (2)
17. **ApproveReturnRequestCommandHandler**
    - Update ReturnRequest status to Approved
    - Credit amount to account balance
    - Create AccountTransaction (Credit)
    - Log approval with remarks
    - Return success

18. **RejectReturnRequestCommandHandler**
    - Update ReturnRequest status to Rejected
    - Log rejection
    - Return success

### Payment Management (3)
19. **CreatePaymentCommandHandler**
    - Create Payment entity
    - Set status to Pending
    - Log submission
    - Return PaymentId

20. **ApprovePaymentCommandHandler**
    - Update Payment status to Approved
    - Optionally credit account (if ApplyToBalance = true)
    - Create AccountTransaction (if applied)
    - Log approval
    - Return success

21. **RejectPaymentCommandHandler**
    - Update Payment status to Rejected
    - Log rejection with reason
    - Return success

---

## Query Handlers Needed (17 total)

### Vehicle Data (4)
1. **GetVehicleModelsQueryHandler**
   - Query all VehicleModels
   - Filter by IsActive
   - Filter by SearchTerm (LIKE ModelName)
   - Map to VehicleModelDto
   - Return IEnumerable<VehicleModelDto>

2. **GetVehicleModelByIdQueryHandler**
   - Query specific model
   - Map to VehicleModelDto
   - Return VehicleModelDto

3. **GetVehicleColorsQueryHandler**
   - Query all VehicleColors
   - Filter by IsActive
   - Filter by SearchTerm
   - Return IEnumerable<VehicleColorDto>

4. **GetVehiclePricesQueryHandler**
   - Query VehiclePriceHistory
   - Filter by ModelId, ColorId, Month, Year
   - Include model/color names
   - Return IEnumerable<VehiclePriceHistoryDto>

### Subdealer Data (3)
5. **GetSubdealersQueryHandler**
   - Query Users where UserRole = Subdealer
   - Filter by IsActive, SearchTerm
   - Map to UserDto
   - Return IEnumerable<UserDto>

6. **GetSubdealerAccountsQueryHandler**
   - Query SubdealerAccounts for specific subdealer
   - Include subdealer name
   - Filter by IsActive
   - Return IEnumerable<SubdealerAccountDto>

7. **GetAccountPermissionsQueryHandler**
   - Query AccountPermissions for account
   - Optionally filter IsAccessibleOnly
   - Return IEnumerable<AccountPermissionDto>

### Account Data (2)
8. **GetAccountBalanceQueryHandler**
   - Query AccountBalance
   - Include subdealer/account names
   - Return AccountBalanceDto

9. **GetAccountTransactionsQueryHandler**
   - Query AccountTransaction history
   - Filter by TransactionType, ReferenceType, DateRange
   - Include initiator name
   - Order by CreatedDate DESC
   - Return IEnumerable<AccountTransactionDto>

### Purchase Orders (2)
10. **GetPurchaseOrdersQueryHandler**
    - Query PurchaseOrders with filtering
    - Filter by SubdealerId, AccountId, Status, DateRange
    - Search by OrderNumber
    - Include subdealer/account names, approver names
    - Order by CreatedDate DESC
    - Return IEnumerable<PurchaseOrderDto>

11. **GetPurchaseOrderByIdQueryHandler**
    - Query specific order with details
    - Include all related data
    - Return PurchaseOrderDto

### Commission Data (2)
12. **GetCommissionsQueryHandler**
    - Query Commissions with filtering
    - Filter by SubdealerId, AccountId, Status, Month/Year, DateRange
    - Include vehicle chassis, subdealer/account names
    - Return IEnumerable<CommissionDto>

13. **GetCommissionRatesQueryHandler**
    - Query CommissionRates
    - Filter by ModelId
    - Filter by ActiveOnly (current month/year)
    - Include model names
    - Return IEnumerable<CommissionRateDto>

### Business Data (3)
14. **GetReturnRequestsQueryHandler**
    - Query ReturnRequests with filtering
    - Filter by AccountId, Status, DateRange
    - Include order/vehicle/account names
    - Return IEnumerable<ReturnRequestDto>

15. **GetPaymentsQueryHandler**
    - Query Payments with filtering
    - Filter by SubdealerId, AccountId, Status, DateRange, AppliedOnly
    - Include subdealer/account names, processor names
    - Return IEnumerable<PaymentDto>

16. **GetAuditLogsQueryHandler**
    - Query AuditLogs with comprehensive filtering
    - Filter by EntityType, EntityId, Action, UserId, UserRole, DateRange
    - Search by Remarks
    - Order by CreatedDate DESC
    - Include user names
    - Return IEnumerable<AuditLogDto>

17. **GetDashboardSummaryQueryHandler**
    - Count subdealers, accounts
    - Sum total balances, reserved amounts
    - Count pending items (orders, commissions, returns, payments)
    - Get recent activities from AuditLog
    - Return DashboardSummary

---

## Implementation Pattern

Each handler follows same pattern:

1. **Dependency Injection**
   - Inject IUnitOfWork (for repositories)
   - Inject IAuditService (for logging)
   - Inject IMapper (for queries)

2. **Business Logic**
   - Validate inputs
   - Check domain constraints
   - Execute operations
   - Handle errors

3. **Data Persistence**
   - Call repositories via _unitOfWork
   - Call SaveChangesAsync()

4. **Audit Logging**
   - Log action with WHO/WHAT/WHEN/WHY
   - Include old/new values for updates
   - Include remarks for approvals/rejections

5. **Return Value**
   - Return ID for creates
   - Return DTO for queries
   - Return bool for updates/deletes

---

## Key Services to Inject

### IUnitOfWork
- Access to all repositories
- Manages database transactions
- Methods: SaveChangesAsync(), BeginTransactionAsync(), CommitTransactionAsync()

### IAuditService
- Log operations automatically
- Methods:
  - LogActionAsync(entityType, entityId, action, userId, userRole, newValue, oldValue, remarks)
  - LogTransactionAsync(accountId, type, amount, balanceAfter, reason, referenceId, referenceType)

### IMapper (AutoMapper)
- Map entities to DTOs
- Configure in MappingProfile

---

## Error Handling

All handlers catch exceptions and throw ApplicationException with context:

```csharp
catch (Exception ex)
{
    throw new ApplicationException($"Error [Action] [Entity]: {ex.Message}", ex);
}
```

This allows controllers to catch and return appropriate HTTP responses.

---

## Testing the Handlers

Each handler can be tested with:

```csharp
[Test]
public async Task Handle_CreateVehicleModel_ReturnsModelId()
{
    // Arrange
    var command = new CreateVehicleModelCommand 
    { 
        ModelName = "Test Model",
        CreatedBy = 1
    };
    var handler = new CreateVehicleModelCommandHandler(_unitOfWork, _auditService);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.IsTrue(result > 0);
}
```

---

## Summary

- 20 Command Handlers: Execute business logic + audit logging
- 17 Query Handlers: Retrieve filtered data + map to DTOs
- All follow same pattern: Validate → Execute → Persist → Audit → Return
- All easily modifiable until testing begins
- Ready for DI container registration


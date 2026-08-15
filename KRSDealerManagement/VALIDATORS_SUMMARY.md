# FluentValidation Validators - Summary & Templates

## Overview

All 20 command validators follow same pattern:
- Validate required fields
- Validate format (email, phone, numeric ranges)
- Validate business constraints (balance, dates)
- Return clear error messages

## Validators to Create (19 remaining after CreateVehicleModelCommandValidator)

### 1. UpdateVehicleModelCommandValidator
```csharp
RuleFor(x => x.ModelId).GreaterThan(0);
RuleFor(x => x.ModelName).NotEmpty().Length(3, 100);
RuleFor(x => x.ModifiedBy).GreaterThan(0);
```

### 2. CreateVehiclePriceCommandValidator
```csharp
RuleFor(x => x.ModelId).GreaterThan(0);
RuleFor(x => x.ColorId).GreaterThan(0);
RuleFor(x => x.Month).InclusiveBetween(1, 12);
RuleFor(x => x.Year).GreaterThanOrEqualTo(DateTime.Now.Year);
RuleFor(x => x.Price).GreaterThan(0);
RuleFor(x => x.CreatedBy).GreaterThan(0);
```

### 3. UpdateVehiclePriceCommandValidator
```csharp
RuleFor(x => x.PriceHistoryId).GreaterThan(0);
RuleFor(x => x.Price).GreaterThan(0);
RuleFor(x => x.ModifiedBy).GreaterThan(0);
```

### 4. CreateCommissionRateCommandValidator
```csharp
RuleFor(x => x.ModelId).GreaterThan(0);
RuleFor(x => x.CommissionAmount).GreaterThan(0);
RuleFor(x => x.StartMonth).InclusiveBetween(1, 12);
RuleFor(x => x.StartYear).GreaterThanOrEqualTo(DateTime.Now.Year);
RuleFor(x => x.CreatedBy).GreaterThan(0);
RuleFor(x => x.ExpiryMonth).InclusiveBetween(1, 12).When(x => x.ExpiryMonth.HasValue);
RuleFor(x => x.ExpiryYear).GreaterThan(x => x.StartYear).When(x => x.ExpiryYear.HasValue);
```

### 5. UpdateCommissionRateCommandValidator
```csharp
RuleFor(x => x.CommissionRateId).GreaterThan(0);
RuleFor(x => x.CommissionAmount).GreaterThan(0);
RuleFor(x => x.ModifiedBy).GreaterThan(0);
```

### 6. CreateSubdealerCommandValidator
```csharp
RuleFor(x => x.SubdealerName)
    .NotEmpty().WithMessage("Subdealer name is required")
    .Length(3, 100);
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress();
RuleFor(x => x.Location)
    .NotEmpty().Length(3, 100);
RuleFor(x => x.PrimaryPhone)
    .NotEmpty()
    .Matches(@"^\d{10}$").WithMessage("Phone must be 10 digits");
RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
RuleFor(x => x.CreatedBy).GreaterThan(0);
```

### 7. CreateSubdealerAccountCommandValidator
```csharp
RuleFor(x => x.SubdealerId).GreaterThan(0);
RuleFor(x => x.AccountName).NotEmpty().Length(3, 100);
RuleFor(x => x.AccountType).NotEmpty().Length(3, 50);
RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
RuleFor(x => x.CreatedBy).GreaterThan(0);
```

### 8. ConfigureAccountPermissionsCommandValidator
```csharp
RuleFor(x => x.AccountId).GreaterThan(0);
RuleFor(x => x.Permissions).NotEmpty().WithMessage("At least one permission required");
RuleForEach(x => x.Permissions).SetValidator(new PermissionSettingValidator());
RuleFor(x => x.ConfiguredBy).GreaterThan(0);
```

### 9. CreatePurchaseOrderCommandValidator
```csharp
RuleFor(x => x.AccountId).GreaterThan(0);
RuleFor(x => x.SubdealerId).GreaterThan(0);
RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item required");
RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());
RuleFor(x => x.CreatedBy).GreaterThan(0);
```

### 10. ApprovePurchaseOrderItemCommandValidator
```csharp
RuleFor(x => x.OrderId).GreaterThan(0);
RuleFor(x => x.VehicleId).GreaterThan(0);
RuleFor(x => x.Amount).GreaterThan(0);
RuleFor(x => x.ApprovedBy).GreaterThan(0);
```

### 11. RejectPurchaseOrderItemCommandValidator
```csharp
RuleFor(x => x.OrderId).GreaterThan(0);
RuleFor(x => x.VehicleId).GreaterThan(0);
RuleFor(x => x.Amount).GreaterThan(0);
RuleFor(x => x.RejectedBy).GreaterThan(0);
```

### 12. SubmitCommissionCommandValidator
```csharp
RuleFor(x => x.AccountId).GreaterThan(0);
RuleFor(x => x.SubdealerId).GreaterThan(0);
RuleFor(x => x.VehicleId).GreaterThan(0);
RuleFor(x => x.ModelId).GreaterThan(0);
RuleFor(x => x.Month).InclusiveBetween(1, 12);
RuleFor(x => x.Year).GreaterThanOrEqualTo(DateTime.Now.Year - 1);
RuleFor(x => x.CommissionAmount).GreaterThan(0);
RuleFor(x => x.SubmittedBy).GreaterThan(0);
```

### 13. ApproveCommissionCommandValidator
```csharp
RuleFor(x => x.CommissionId).GreaterThan(0);
RuleFor(x => x.ApprovedBy).GreaterThan(0);
```

### 14. CreateReturnRequestCommandValidator
```csharp
RuleFor(x => x.AccountId).GreaterThan(0);
RuleFor(x => x.OrderId).GreaterThan(0);
RuleFor(x => x.VehicleId).GreaterThan(0);
RuleFor(x => x.RefundAmount).GreaterThan(0);
RuleFor(x => x.CreatedBy).GreaterThan(0);
```

### 15. ApproveReturnRequestCommandValidator
```csharp
RuleFor(x => x.ReturnRequestId).GreaterThan(0);
RuleFor(x => x.ApprovedBy).GreaterThan(0);
```

### 16. RejectReturnRequestCommandValidator
```csharp
RuleFor(x => x.ReturnRequestId).GreaterThan(0);
RuleFor(x => x.RejectedBy).GreaterThan(0);
```

### 17. CreatePaymentCommandValidator
```csharp
RuleFor(x => x.AccountId).GreaterThan(0);
RuleFor(x => x.SubdealerId).GreaterThan(0);
RuleFor(x => x.Amount).GreaterThan(0);
RuleFor(x => x.PaymentType)
    .NotEmpty()
    .Must(x => new[] { "Cash", "GPay", "NEFT", "Others" }.Contains(x))
    .WithMessage("Invalid payment type");
RuleFor(x => x.PaymentDate).NotEmpty();
RuleFor(x => x.CreatedBy).GreaterThan(0);
```

### 18. ApprovePaymentCommandValidator
```csharp
RuleFor(x => x.PaymentId).GreaterThan(0);
RuleFor(x => x.ApprovedBy).GreaterThan(0);
```

### 19. RejectPaymentCommandValidator
```csharp
RuleFor(x => x.PaymentId).GreaterThan(0);
RuleFor(x => x.RejectedBy).GreaterThan(0);
```

## Nested Validators

### PermissionSettingValidator
```csharp
public class PermissionSettingValidator : AbstractValidator<PermissionSetting>
{
    public PermissionSettingValidator()
    {
        RuleFor(x => x.MenuKey).NotEmpty();
        RuleFor(x => x.MenuName).NotEmpty();
    }
}
```

### OrderItemValidator
```csharp
public class OrderItemValidator : AbstractValidator<OrderItem>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ModelId).GreaterThan(0);
        RuleFor(x => x.ColorId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
    }
}
```

## Registration in DI Container

In **DependencyInjection.cs**:

```csharp
services.AddValidatorsFromAssemblyContaining<CreateVehicleModelCommandValidator>();
```

This auto-registers all validators inheriting from AbstractValidator<T>.

## Pipeline Behavior for Validation

Add to **Program.cs**:

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining(typeof(CreateVehicleModelCommand));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // Auto-validates commands
});
```

Then create **ValidationBehavior.cs**:

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IValidator<TRequest>[] _validators;

    public ValidationBehavior(IValidator<TRequest>[] validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = _validators
            .SelectMany(v => v.Validate(request).Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

## File Count

- 1 created: CreateVehicleModelCommandValidator.cs ✅
- 19 remaining to create
- 2 nested validators: PermissionSettingValidator, OrderItemValidator
- 1 pipeline behavior: ValidationBehavior.cs

Total: 23 files for validation setup

## Next Steps

1. Create 19 remaining validators (5 minutes each using templates)
2. Create 2 nested validators
3. Create ValidationBehavior.cs
4. Register in DependencyInjection.cs
5. All easily modifiable until testing


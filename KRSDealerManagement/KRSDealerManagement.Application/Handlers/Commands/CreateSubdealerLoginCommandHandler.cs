using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateSubdealerLoginCommandHandler : IRequestHandler<CreateSubdealerLoginCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateSubdealerLoginCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateSubdealerLoginCommand request, CancellationToken cancellationToken)
        {
            var org = await _unitOfWork.SubDealers.GetByIdAsync(request.SubDealerId);
            if (org == null || !org.IsActive)
                throw new InvalidOperationException("Subdealer not found or inactive.");

            var username = request.Username.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("Username is required.");

            var duplicate = (await _unitOfWork.Users.GetAllAsync())
                .Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
                throw new InvalidOperationException("Username is already taken.");

            var subRole = (await _unitOfWork.Roles.GetAllAsync())
                .FirstOrDefault(r => r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("SUBDEALER role missing from Roles table.");

            var existingLogins = await SubdealerOrgService.GetLoginsForOrgAsync(_unitOfWork, request.SubDealerId);
            var isFirstLogin = existingLogins.Count == 0;

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                    ? username
                    : request.DisplayName.Trim();

                var userId = await _unitOfWork.Users.AddAsync(new User
                {
                    Username = username,
                    Email = org.Email ?? $"{username}@krs.com",
                    PasswordHash = request.Password.Trim(),
                    FirstName = displayName,
                    LastName = org.Location ?? "",
                    UserRole = 2,
                    PhoneNumber = org.PrimaryPhone,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.UserOrgRoles.AddAsync(new UserOrgRole
                {
                    UserId = userId,
                    RoleId = subRole.RoleId,
                    DealershipId = org.DealershipId,
                    SubDealerId = request.SubDealerId,
                    IsPrimary = isFirstLogin,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });

                int permissionAccountId;
                if (isFirstLogin)
                {
                    permissionAccountId = await _unitOfWork.SubdealerAccounts.AddAsync(new SubdealerAccount
                    {
                        SubdealerId = userId,
                        AccountName = "Main Account",
                        AccountType = "Main",
                        Description = $"Main wallet for {org.SubDealerName}",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                    await _unitOfWork.SaveChangesAsync();

                    await _unitOfWork.AccountBalances.AddAsync(new AccountBalance
                    {
                        SubdealerAccountId = permissionAccountId,
                        SubdealerId = userId,
                        CurrentBalance = request.InitialBalance,
                        ReservedAmount = 0,
                        AvailableBalance = request.InitialBalance,
                        InitialBalance = request.InitialBalance,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });

                    if (request.InitialBalance > 0)
                    {
                        await _auditService.LogTransactionAsync(
                            accountId: permissionAccountId,
                            transactionType: 2,
                            amount: request.InitialBalance,
                            balanceAfter: request.InitialBalance,
                            reason: "Initial balance on first login",
                            referenceType: "AccountCreation",
                            referenceId: permissionAccountId,
                            initiatedBy: request.CreatedBy);
                    }
                }
                else
                {
                    permissionAccountId = await _unitOfWork.SubdealerAccounts.AddAsync(new SubdealerAccount
                    {
                        SubdealerId = userId,
                        AccountName = $"{displayName} Login",
                        AccountType = "Login",
                        Description = $"Login permissions for {username} ({org.SubDealerName})",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                    await _unitOfWork.SaveChangesAsync();
                }

                await ApplyPermissionsAsync(permissionAccountId, request.AccessibleMenuKeys);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "SubdealerLogin",
                    entityId: userId,
                    action: "Create",
                    userId: request.CreatedBy,
                    userRole: "Staff",
                    newValue: JsonSerializer.Serialize(new
                    {
                        request.SubDealerId,
                        Username = username,
                        IsPrimary = isFirstLogin,
                        PermissionAccountId = permissionAccountId
                    }));

                return userId;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error creating login: {ex.Message}", ex);
            }
        }

        private async Task ApplyPermissionsAsync(int accountId, List<string>? accessibleMenuKeys)
        {
            var defaultMenus = MenuKeys.GetSubdealerConfigurableMenus();
            var allowed = new HashSet<string>(
                accessibleMenuKeys ?? defaultMenus.Select(m => m.Key),
                StringComparer.OrdinalIgnoreCase);

            foreach (var menu in defaultMenus)
            {
                bool accessible = allowed.Contains(menu.Key);
                await _unitOfWork.AccountPermissions.AddAsync(new AccountPermission
                {
                    AccountId = accountId,
                    MenuKey = menu.Key,
                    MenuName = menu.Name,
                    IsAccessible = accessible,
                    CanCreate = accessible,
                    CanEdit = accessible,
                    CanDelete = false,
                    CanApprove = false,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
            }
        }
    }
}

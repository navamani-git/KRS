using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Creates SubDealer org + login user + UserOrgRole + wallet + AccountPermissions.
    /// </summary>
    public class CreateSubdealerCommandHandler : IRequestHandler<CreateSubdealerCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateSubdealerCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateSubdealerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId);
                if (dealership == null || !dealership.IsActive)
                    throw new InvalidOperationException("Dealership location not found or inactive.");

                var subRole = (await _unitOfWork.Roles.GetAllAsync())
                    .FirstOrDefault(r => r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
                if (subRole == null)
                    throw new InvalidOperationException("SUBDEALER role missing from Roles table.");

                var password = request.Password.Trim();
                var username = GenerateUsername(request.SubdealerName);

                // 1) Business org
                var subDealerId = await _unitOfWork.SubDealers.AddAsync(new SubDealer
                {
                    DealershipId = request.DealershipId,
                    SubDealerCode = username,
                    SubDealerName = request.SubdealerName.Trim(),
                    Location = request.Location.Trim(),
                    PrimaryPhone = request.PrimaryPhone.Trim(),
                    SecondaryPhone = request.SecondaryPhone?.Trim(),
                    SalesRepMobile = request.SalesRepMobile?.Trim(),
                    ServiceRepMobile = request.ServiceRepMobile?.Trim(),
                    Email = request.Email,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });

                // 2) Login user (SubdealerId on orders/accounts still = UserId)
                var userId = await _unitOfWork.Users.AddAsync(new User
                {
                    Username = username,
                    Email = request.Email,
                    PasswordHash = password,
                    FirstName = request.SubdealerName,
                    LastName = request.Location,
                    UserRole = 2,
                    PhoneNumber = request.PrimaryPhone,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();

                // 3) Hierarchy mapping
                await _unitOfWork.UserOrgRoles.AddAsync(new UserOrgRole
                {
                    UserId = userId,
                    RoleId = subRole.RoleId,
                    DealershipId = request.DealershipId,
                    SubDealerId = subDealerId,
                    IsPrimary = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });

                // 4) Wallet
                var accountId = await _unitOfWork.SubdealerAccounts.AddAsync(new SubdealerAccount
                {
                    SubdealerId = userId,
                    AccountName = "Main Account",
                    AccountType = "Main",
                    Description = $"Main account for {request.SubdealerName} ({dealership.DealershipCode})",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.AccountBalances.AddAsync(new AccountBalance
                {
                    SubdealerAccountId = accountId,
                    SubdealerId = userId,
                    CurrentBalance = request.InitialBalance,
                    ReservedAmount = 0,
                    AvailableBalance = request.InitialBalance,
                    InitialBalance = request.InitialBalance,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });

                // 5) AccountPermissions from selected menus (or role defaults)
                var defaultMenus = MenuKeys.GetSubdealerConfigurableMenus();
                var allowed = new HashSet<string>(
                    request.AccessibleMenuKeys ?? defaultMenus.Select(m => m.Key),
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

                if (request.InitialBalance > 0)
                {
                    await _auditService.LogTransactionAsync(
                        accountId: accountId,
                        transactionType: 2,
                        amount: request.InitialBalance,
                        balanceAfter: request.InitialBalance,
                        reason: "Initial balance on account creation",
                        referenceType: "AccountCreation",
                        referenceId: accountId,
                        initiatedBy: request.CreatedBy
                    );
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "SubDealer",
                    entityId: subDealerId,
                    action: "Create",
                    userId: request.CreatedBy,
                    userRole: "Staff",
                    newValue: JsonSerializer.Serialize(new
                    {
                        SubDealerId = subDealerId,
                        LoginUserId = userId,
                        Username = username,
                        DealershipId = request.DealershipId,
                        Dealership = dealership.DealershipCode,
                        Name = request.SubdealerName
                    })
                );

                return userId;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error creating subdealer: {ex.Message}", ex);
            }
        }

        private static string GenerateUsername(string name)
        {
            var username = name.ToLower()
                .Replace(" ", "_")
                .Replace(".", "")
                .Replace(",", "");
            return username.Length > 30 ? username[..30] : username;
        }
    }
}

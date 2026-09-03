using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Results;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IRoleTemplateService _roleTemplateService;
        private readonly PasswordHasher<string> _passwordHasher = new();

        public LoginCommandHandler(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IRoleTemplateService roleTemplateService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();
                var user = users.FirstOrDefault(u =>
                    u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                    return Result<LoginResult>.Failure("Invalid username or password");

                if (!user.IsActive)
                    return Result<LoginResult>.Failure("Your account has been deactivated. Please contact administrator.");

                if (!VerifyPassword(user.PasswordHash, request.Password))
                {
                    await _auditService.LogActionAsync(
                        entityType: "User",
                        entityId: user.UserId,
                        action: "Login_Failed",
                        userId: user.UserId,
                        userRole: "Unknown",
                        newValue: JsonSerializer.Serialize(new { Username = request.Username }),
                        oldValue: null
                    );
                    return Result<LoginResult>.Failure("Invalid username or password");
                }

                var assignment = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                    .Where(a => a.UserId == user.UserId && a.IsActive)
                    .OrderByDescending(a => a.IsPrimary)
                    .FirstOrDefault();

                var roles = await _unitOfWork.Roles.GetAllAsync();
                var role = assignment != null
                    ? roles.FirstOrDefault(r => r.RoleId == assignment.RoleId)
                    : roles.FirstOrDefault(r => MapLegacyRoleCode(user.UserRole).Equals(r.RoleCode, StringComparison.OrdinalIgnoreCase));

                if (role == null)
                    return Result<LoginResult>.Failure("No role assignment found. Contact administrator.");

                string? dealershipName = null;
                if (assignment?.DealershipId is int did)
                {
                    var d = await _unitOfWork.Dealerships.GetByIdAsync(did);
                    dealershipName = d?.DealershipName;
                }

                var menus = await MenuAccessResolver.ResolveAsync(_unitOfWork, user.UserId, role);
                var menuAccess = await MenuAccessResolver.ResolveMapAsync(_unitOfWork, user.UserId, role);

                int legacyRole = _roleTemplateService.MapTemplateToLegacyUserRole(role.RoleTemplateCode ?? role.RoleCode);
                if (role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)) legacyRole = 1;
                else if (role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase)) legacyRole = 2;
                if (user.UserRole != legacyRole)
                {
                    user.UserRole = legacyRole;
                    user.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.Users.UpdateAsync(user);
                }

                var loginResult = new LoginResult
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    UserRole = legacyRole,
                    RoleName = role.RoleName,
                    RoleCode = role.RoleCode,
                    DealershipId = assignment?.DealershipId,
                    DealershipName = dealershipName,
                    SubDealerId = assignment?.SubDealerId,
                    AccessibleMenuKeys = menus,
                    MenuAccess = menuAccess,
                    IsActive = user.IsActive,
                    CanExport = user.CanExport,
                    QuickActionKeys = user.QuickActionKeys,
                    DashboardWidgetKeys = user.DashboardWidgetKeys
                };

                await _auditService.LogActionAsync(
                    entityType: "User",
                    entityId: user.UserId,
                    action: "Login_Success",
                    userId: user.UserId,
                    userRole: role.RoleCode,
                    newValue: JsonSerializer.Serialize(new
                    {
                        Username = user.Username,
                        RoleCode = role.RoleCode,
                        DealershipId = assignment?.DealershipId,
                        LoginTime = DateTime.UtcNow
                    })
                );

                return Result<LoginResult>.Success(loginResult);
            }
            catch (Exception ex)
            {
                return Result<LoginResult>.Failure($"Login failed: {ex.Message}");
            }
        }

        private static string MapLegacyRoleCode(int userRole) => userRole switch
        {
            1 => RoleCodes.SystemAdmin,
            2 => RoleCodes.Subdealer,
            3 => RoleCodes.FinanceAdmin,
            4 => RoleCodes.BranchManager,
            _ => RoleCodes.Subdealer
        };

        private static int MapRoleCodeToLegacy(string roleCode)
        {
            if (roleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)) return 1;
            if (roleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase)) return 2;
            if (roleCode.Equals(RoleCodes.FinanceAdmin, StringComparison.OrdinalIgnoreCase)) return 3;
            if (roleCode.Equals(RoleCodes.BranchManager, StringComparison.OrdinalIgnoreCase)) return 4;
            return 2;
        }

        private bool VerifyPassword(string storedHash, string enteredPassword)
        {
            storedHash = storedHash?.Trim() ?? string.Empty;
            enteredPassword = enteredPassword?.Trim() ?? string.Empty;

            if (storedHash.StartsWith("AQAA", StringComparison.Ordinal))
            {
                var result = _passwordHasher.VerifyHashedPassword(null!, storedHash, enteredPassword);
                return result == PasswordVerificationResult.Success
                    || result == PasswordVerificationResult.SuccessRehashNeeded;
            }

            // Seeded/plain passwords (e.g. KARUR@123) — allow case-insensitive match
            return string.Equals(storedHash, enteredPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
}

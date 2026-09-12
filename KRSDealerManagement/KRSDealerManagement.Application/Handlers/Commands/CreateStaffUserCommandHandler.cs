using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateStaffUserCommandHandler : IRequestHandler<CreateStaffUserCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IRoleTemplateService _roleTemplateService;

        public CreateStaffUserCommandHandler(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IRoleTemplateService roleTemplateService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<int> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
        {
            await StaffDealershipService.ValidateStaffRoleAsync(_unitOfWork, request.RoleId);
            await StaffDealershipService.ValidateDealershipsAsync(_unitOfWork, request.DealershipIds);

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId)
                ?? throw new InvalidOperationException("Role not found.");

            var username = request.Username.Trim().ToLowerInvariant();
            var existing = (await _unitOfWork.Users.GetAllAsync())
                .Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (existing)
                throw new InvalidOperationException($"Username '{username}' is already taken.");

            var legacyRole = _roleTemplateService.MapTemplateToLegacyUserRole(role.RoleTemplateCode);
            var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : role.RoleName;
            var primaryDealershipId = request.DealershipIds.First();

            var userId = await _unitOfWork.Users.AddAsync(new User
            {
                Username = username,
                Email = string.IsNullOrWhiteSpace(request.Email) ? $"{username}@krs.local" : request.Email.Trim(),
                PasswordHash = request.Password.Trim(),
                FirstName = firstName,
                LastName = lastName,
                UserRole = legacyRole,
                PhoneNumber = request.PhoneNumber?.Trim() ?? "",
                IsActive = true,
                CanExport = request.CanExport,
                CanViewStatement = request.CanViewStatement,
                CanEditWarrantyClaims = request.CanEditWarrantyClaims,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            await _unitOfWork.UserOrgRoles.AddAsync(new UserOrgRole
            {
                UserId = userId,
                RoleId = role.RoleId,
                DealershipId = primaryDealershipId,
                SubDealerId = null,
                IsPrimary = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            await StaffDealershipService.ReplaceUserDealershipsAsync(
                _unitOfWork, userId, request.DealershipIds, isActive: true);

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "StaffUser",
                entityId: userId,
                action: "Create",
                userId: request.CreatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    userId,
                    username,
                    role.RoleId,
                    role.RoleCode,
                    DealershipIds = request.DealershipIds,
                    request.FullName
                }));

            return userId;
        }
    }
}

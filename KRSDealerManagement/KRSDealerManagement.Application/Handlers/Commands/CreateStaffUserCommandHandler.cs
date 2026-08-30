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
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId)
                ?? throw new InvalidOperationException("Role not found.");

            if (!role.IsActive || role.IsSystemRole
                || role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select a valid staff role.");

            if (!role.DealershipId.HasValue || role.DealershipId.Value != request.DealershipId)
                throw new InvalidOperationException("Selected role does not belong to the chosen dealership.");

            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId);
            if (dealership == null || !dealership.IsActive)
                throw new InvalidOperationException("Dealership not found or inactive.");

            var username = request.Username.Trim().ToLowerInvariant();
            var existing = (await _unitOfWork.Users.GetAllAsync())
                .Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (existing)
                throw new InvalidOperationException($"Username '{username}' is already taken.");

            var legacyRole = _roleTemplateService.MapTemplateToLegacyUserRole(role.RoleTemplateCode);
            var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : role.RoleName;

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
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

            await _unitOfWork.UserOrgRoles.AddAsync(new UserOrgRole
            {
                UserId = userId,
                RoleId = role.RoleId,
                DealershipId = request.DealershipId,
                SubDealerId = null,
                IsPrimary = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });

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
                    request.DealershipId,
                    Dealership = dealership.DealershipCode,
                    request.FullName
                }));

            return userId;
        }
    }
}

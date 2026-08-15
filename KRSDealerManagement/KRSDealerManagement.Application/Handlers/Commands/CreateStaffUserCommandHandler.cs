using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateStaffUserCommandHandler : IRequestHandler<CreateStaffUserCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateStaffUserCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
        {
            if (request.StaffRole is not ((int)UserRoleEnum.FinanceAdmin) and not ((int)UserRoleEnum.DealerBranchManager))
                throw new InvalidOperationException("Only Finance Admin or Branch Manager can be created here.");

            var dealership = await _unitOfWork.Dealerships.GetByIdAsync(request.DealershipId);
            if (dealership == null || !dealership.IsActive)
                throw new InvalidOperationException("Dealership not found or inactive.");

            var roleCode = request.StaffRole == (int)UserRoleEnum.FinanceAdmin
                ? RoleCodes.FinanceAdmin
                : RoleCodes.BranchManager;

            var role = (await _unitOfWork.Roles.GetAllAsync())
                .FirstOrDefault(r => r.RoleCode.Equals(roleCode, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Role {roleCode} not found in Roles table.");

            var username = request.Username.Trim().ToLowerInvariant();
            var existing = (await _unitOfWork.Users.GetAllAsync())
                .Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (existing)
                throw new InvalidOperationException($"Username '{username}' is already taken.");

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
                UserRole = request.StaffRole,
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
                    request.StaffRole,
                    RoleCode = roleCode,
                    request.DealershipId,
                    Dealership = dealership.DealershipCode,
                    request.FullName
                }));

            return userId;
        }
    }
}

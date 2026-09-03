using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetStaffUsersQueryHandler : IRequestHandler<GetStaffUsersQuery, IEnumerable<StaffUserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStaffUsersQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<StaffUserDto>> Handle(GetStaffUsersQuery request, CancellationToken cancellationToken)
        {
            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToList();
            var staffRoleIds = roles
                .Where(r => r.IsActive
                    && !r.IsSystemRole
                    && !r.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                    && !r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.RoleId)
                .ToHashSet();

            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.IsActive && staffRoleIds.Contains(a.RoleId))
                .ToList();

            if (request.DealershipId.HasValue)
                assignments = assignments.Where(a => a.DealershipId == request.DealershipId.Value).ToList();

            if (request.RoleId.HasValue)
                assignments = assignments.Where(a => a.RoleId == request.RoleId.Value).ToList();

            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);

            var result = assignments
                .Where(a => users.ContainsKey(a.UserId))
                .Select(a =>
                {
                    var user = users[a.UserId];
                    var role = roles.FirstOrDefault(r => r.RoleId == a.RoleId);
                    dealerships.TryGetValue(a.DealershipId ?? 0, out var dealership);
                    return new StaffUserDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        FullName = user.GetFullName(),
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        UserRole = user.UserRole,
                        RoleId = role?.RoleId,
                        RoleName = role?.RoleName ?? "Staff",
                        DealershipId = a.DealershipId,
                        DealershipName = dealership?.DealershipName,
                    IsActive = user.IsActive,
                    CanExport = user.CanExport,
                    PasswordHash = user.PasswordHash,
                        CreatedDate = user.CreatedDate
                    };
                });

            if (request.IsActive.HasValue)
                result = result.Where(u => u.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                result = result.Where(u =>
                    u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || u.Username.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (u.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (u.DealershipName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (u.RoleName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return result.OrderBy(u => u.RoleName).ThenBy(u => u.FullName).ToList();
        }
    }
}

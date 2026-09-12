using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
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
            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);

            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToList();
            var staffRoleIds = roles
                .Where(r => r.IsActive
                    && !r.IsSystemRole
                    && !r.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                    && !r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.RoleId)
                .ToHashSet();

            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => staffRoleIds.Contains(a.RoleId))
                .GroupBy(a => a.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(a => a.IsActive).ThenByDescending(a => a.UserOrgRoleId).First());

            var userDealerships = (await _unitOfWork.UserDealerships.GetAllAsync())
                .Where(ud => ud.IsActive)
                .GroupBy(ud => ud.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ud => ud.DealershipId).Distinct().ToList());

            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);

            IEnumerable<int> staffUserIds = assignments.Keys;

            if (dealershipFilter != null)
            {
                staffUserIds = staffUserIds.Where(userId =>
                {
                    if (userDealerships.TryGetValue(userId, out var ids))
                        return ids.Any(id => dealershipFilter.Contains(id));

                    return assignments.TryGetValue(userId, out var assignment)
                        && assignment.DealershipId.HasValue
                        && dealershipFilter.Contains(assignment.DealershipId.Value);
                });
            }

            if (request.RoleId.HasValue)
                staffUserIds = staffUserIds.Where(id => assignments[id].RoleId == request.RoleId.Value);

            var result = staffUserIds
                .Where(users.ContainsKey)
                .Select(userId =>
                {
                    var user = users[userId];
                    var assignment = assignments[userId];
                    var role = roles.FirstOrDefault(r => r.RoleId == assignment.RoleId);

                    var dealerIds = userDealerships.TryGetValue(userId, out var ids) && ids.Count > 0
                        ? ids
                        : assignment.DealershipId.HasValue
                            ? new List<int> { assignment.DealershipId.Value }
                            : new List<int>();

                    var dealerNames = dealerIds
                        .Select(id => dealerships.TryGetValue(id, out var d) ? d.DealershipName : null)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Cast<string>()
                        .ToList();

                    dealerships.TryGetValue(assignment.DealershipId ?? 0, out var legacyDealership);

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
                        DealershipId = dealerIds.FirstOrDefault() is int first && first > 0 ? first : assignment.DealershipId,
                        DealershipName = dealerNames.FirstOrDefault() ?? legacyDealership?.DealershipName,
                        DealershipIds = dealerIds,
                        DealershipNames = dealerNames.Count > 0 ? string.Join(", ", dealerNames) : null,
                        IsActive = user.IsActive,
                        CanExport = user.CanExport,
                        CanViewStatement = user.CanViewStatement,
                        CanEditWarrantyClaims = user.CanEditWarrantyClaims,
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
                    || (u.DealershipNames?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (u.RoleName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return result.OrderBy(u => u.RoleName).ThenBy(u => u.FullName).ToList();
        }
    }
}

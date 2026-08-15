using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetStaffUsersQueryHandler : IRequestHandler<GetStaffUsersQuery, IEnumerable<StaffUserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStaffUsersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<StaffUserDto>> Handle(GetStaffUsersQuery request, CancellationToken cancellationToken)
        {
            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToList();
            var financeRole = roles.FirstOrDefault(r => r.RoleCode.Equals(RoleCodes.FinanceAdmin, StringComparison.OrdinalIgnoreCase));
            var branchRole = roles.FirstOrDefault(r => r.RoleCode.Equals(RoleCodes.BranchManager, StringComparison.OrdinalIgnoreCase));
            var staffRoleIds = new HashSet<int>();
            if (financeRole != null) staffRoleIds.Add(financeRole.RoleId);
            if (branchRole != null) staffRoleIds.Add(branchRole.RoleId);

            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.IsActive && staffRoleIds.Contains(a.RoleId))
                .ToList();

            if (request.DealershipId.HasValue)
                assignments = assignments.Where(a => a.DealershipId == request.DealershipId.Value).ToList();

            if (request.StaffRole.HasValue)
            {
                var roleId = request.StaffRole.Value switch
                {
                    (int)UserRoleEnum.FinanceAdmin => financeRole?.RoleId,
                    (int)UserRoleEnum.DealerBranchManager => branchRole?.RoleId,
                    _ => null
                };
                if (roleId.HasValue)
                    assignments = assignments.Where(a => a.RoleId == roleId.Value).ToList();
            }

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
                        RoleName = role?.RoleName ?? user.UserRole switch
                        {
                            3 => "Finance Admin",
                            4 => "Branch Manager",
                            _ => "Staff"
                        },
                        DealershipId = a.DealershipId,
                        DealershipName = dealership?.DealershipName,
                        IsActive = user.IsActive,
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
                    || (u.DealershipName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return result.OrderBy(u => u.RoleName).ThenBy(u => u.FullName).ToList();
        }
    }
}

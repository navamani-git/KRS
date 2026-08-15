using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetSubdealersQueryHandler : IRequestHandler<GetSubdealersQuery, IEnumerable<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<UserDto>> Handle(GetSubdealersQuery request, CancellationToken cancellationToken)
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var roles = await _unitOfWork.Roles.GetAllAsync();
            var subRole = roles.FirstOrDefault(r => r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => subRole == null || a.RoleId == subRole.RoleId)
                .ToList();

            var subdealerUserIds = assignments
                .Where(a => !request.DealershipId.HasValue || a.DealershipId == request.DealershipId)
                .Select(a => a.UserId)
                .ToHashSet();

            // Prefer UserOrgRoles; fall back to legacy UserRole=2 if no assignment yet
            var subdealers = users.Where(u =>
                subdealerUserIds.Contains(u.UserId) ||
                (u.UserRole == 2 && !assignments.Any(a => a.UserId == u.UserId) && !request.DealershipId.HasValue));

            if (request.IsActive.HasValue)
                subdealers = subdealers.Where(u => u.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                subdealers = subdealers.Where(u =>
                    (u.FirstName ?? "").Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.LastName ?? "").Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.Username ?? "").Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email ?? "").Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.PhoneNumber ?? "").Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));

            return subdealers.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                FirstName = u.FirstName,
                LastName = u.LastName ?? "",
                UserRole = u.UserRole,
                PhoneNumber = u.PhoneNumber ?? "",
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                ModifiedDate = u.ModifiedDate
            }).OrderBy(u => u.FirstName).ToList();
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetSubdealersQueryHandler : IRequestHandler<GetSubdealersQuery, IEnumerable<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealersQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<UserDto>> Handle(GetSubdealersQuery request, CancellationToken cancellationToken)
        {
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).AsEnumerable();
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            if (request.DealershipId.HasValue)
                orgs = orgs.Where(o => o.DealershipId == request.DealershipId.Value);

            if (request.IsActive.HasValue)
                orgs = orgs.Where(o => o.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.District))
            {
                var district = request.District.Trim();
                orgs = orgs.Where(o =>
                    dealerships.TryGetValue(o.DealershipId, out var dealer)
                    && string.Equals(dealer.Location?.Trim(), district, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                orgs = orgs.Where(o =>
                    (o.SubDealerName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (o.Location ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (o.Email ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (o.PrimaryPhone ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (dealerships.TryGetValue(o.DealershipId, out var dealer)
                        && (dealer.Location ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var result = new List<UserDto>();

            foreach (var org in orgs.OrderBy(o => o.SubDealerName))
            {
                dealerships.TryGetValue(org.DealershipId, out var dealership);
                var logins = await SubdealerOrgService.GetLoginsForOrgAsync(_unitOfWork, org.SubDealerId);
                var activeLogins = logins.Where(l => l.IsActive).ToList();
                var primaryAssignment = activeLogins.OrderByDescending(l => l.IsPrimary).FirstOrDefault()
                    ?? logins.OrderByDescending(l => l.IsPrimary).FirstOrDefault();

                User? primaryUser = null;
                if (primaryAssignment != null && users.TryGetValue(primaryAssignment.UserId, out var pu))
                    primaryUser = pu;

                result.Add(new UserDto
                {
                    UserId = primaryUser?.UserId ?? 0,
                    SubDealerId = org.SubDealerId,
                    LoginCount = logins.Count,
                    Username = primaryUser?.Username ?? "—",
                    Email = org.Email ?? primaryUser?.Email ?? "",
                    PasswordHash = primaryUser?.PasswordHash,
                    FirstName = org.SubDealerName,
                    LastName = org.Location ?? "",
                    District = dealership?.Location?.Trim(),
                    UserRole = 2,
                    PhoneNumber = org.PrimaryPhone ?? "",
                    IsActive = org.IsActive,
                    CreatedDate = org.CreatedDate,
                    ModifiedDate = org.ModifiedDate
                });
            }

            if (request.ColumnFilters is { Count: > 0 } cf)
            {
                result = result.Where(s =>
                    GridFilterHelper.MatchesContains(s.GetFullName(), GridFilterHelper.GetFilter(cf, "name"))
                    && GridFilterHelper.MatchesContains(s.Email, GridFilterHelper.GetFilter(cf, "email"))
                    && GridFilterHelper.MatchesContains(s.District, GridFilterHelper.GetFilter(cf, "district"))
                    && GridFilterHelper.MatchesContains(s.LastName, GridFilterHelper.GetFilter(cf, "location"))
                    && GridFilterHelper.MatchesContains(s.PhoneNumber, GridFilterHelper.GetFilter(cf, "phone"))
                    && GridFilterHelper.MatchesContains(s.IsActive ? "Active" : "Inactive", GridFilterHelper.GetFilter(cf, "status"))
                    && GridFilterHelper.MatchesDate(s.CreatedDate, GridFilterHelper.GetDateFilter(cf, "created"), GridFilterHelper.GetDateFilter(cf, "created")))
                    .ToList();
            }

            return result;
        }
    }
}

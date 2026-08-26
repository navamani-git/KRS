using MediatR;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Queries
{
    public class GetUserAccessContextQuery : IRequest<UserAccessContext?>
    {
        public int UserId { get; set; }
    }

    public class UserAccessContext
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
        public int? DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public int? SubDealerId { get; set; }
        public string? SubDealerName { get; set; }
        public List<string> AccessibleMenuKeys { get; set; } = new();
        public Dictionary<string, MenuAccessLevel> MenuAccess { get; set; } = new();
        public bool IsSystemAdmin => RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase);
        public bool IsBranchManager => RoleCode.Equals(RoleCodes.BranchManager, StringComparison.OrdinalIgnoreCase);
        public bool IsFinanceAdmin => RoleCode.Equals(RoleCodes.FinanceAdmin, StringComparison.OrdinalIgnoreCase);
        public bool IsSubdealer => RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase);
    }

    public class GetUserAccessContextQueryHandler : IRequestHandler<GetUserAccessContextQuery, UserAccessContext?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserAccessContextQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserAccessContext?> Handle(GetUserAccessContextQuery request, CancellationToken cancellationToken)
        {
            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == request.UserId && a.IsActive)
                .OrderByDescending(a => a.IsPrimary)
                .ThenBy(a => a.UserOrgRoleId)
                .ToList();

            if (!assignments.Any()) return null;

            var assignment = assignments.First();
            var role = (await _unitOfWork.Roles.GetAllAsync()).FirstOrDefault(r => r.RoleId == assignment.RoleId && r.IsActive);
            if (role == null) return null;

            string? dealershipName = null;
            if (assignment.DealershipId.HasValue)
            {
                var d = await _unitOfWork.Dealerships.GetByIdAsync(assignment.DealershipId.Value);
                dealershipName = d?.DealershipName;
            }

            string? subDealerName = null;
            if (assignment.SubDealerId.HasValue)
            {
                var s = await _unitOfWork.SubDealers.GetByIdAsync(assignment.SubDealerId.Value);
                subDealerName = s?.SubDealerName;
            }

            var menus = await MenuAccessResolver.ResolveAsync(_unitOfWork, request.UserId, role);
            var menuAccess = await MenuAccessResolver.ResolveMapAsync(_unitOfWork, request.UserId, role);

            return new UserAccessContext
            {
                UserId = request.UserId,
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                DealershipId = assignment.DealershipId,
                DealershipName = dealershipName,
                SubDealerId = assignment.SubDealerId,
                SubDealerName = subDealerName,
                AccessibleMenuKeys = menus,
                MenuAccess = menuAccess
            };
        }
    }

    public class GetDealershipsQuery : IRequest<IEnumerable<DealershipDto>>
    {
        public bool? IsActive { get; set; }
    }

    public class DealershipDto
    {
        public int DealershipId { get; set; }
        public string DealershipCode { get; set; } = "";
        public string DealershipName { get; set; } = "";
        public string? Location { get; set; }
        public string? ContactPhone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public int SubDealerCount { get; set; }
    }

    public class GetDealershipsQueryHandler : IRequestHandler<GetDealershipsQuery, IEnumerable<DealershipDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetDealershipsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<DealershipDto>> Handle(GetDealershipsQuery request, CancellationToken cancellationToken)
        {
            var dealers = await _unitOfWork.Dealerships.GetAllAsync();
            var subs = (await _unitOfWork.SubDealers.GetAllAsync()).ToList();
            var q = dealers.AsEnumerable();
            if (request.IsActive.HasValue) q = q.Where(d => d.IsActive == request.IsActive.Value);

            return q.OrderBy(d => d.DealershipName).Select(d => new DealershipDto
            {
                DealershipId = d.DealershipId,
                DealershipCode = d.DealershipCode,
                DealershipName = d.DealershipName,
                Location = d.Location,
                ContactPhone = d.ContactPhone,
                Email = d.Email,
                IsActive = d.IsActive,
                SubDealerCount = subs.Count(s => s.DealershipId == d.DealershipId && s.IsActive)
            }).ToList();
        }
    }
}

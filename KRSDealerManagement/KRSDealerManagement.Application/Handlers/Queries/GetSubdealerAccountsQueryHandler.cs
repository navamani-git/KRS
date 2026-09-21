using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetSubdealerAccountsQueryHandler : IRequestHandler<GetSubdealerAccountsQuery, IEnumerable<SubdealerAccountDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSubdealerAccountsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SubdealerAccountDto>> Handle(GetSubdealerAccountsQuery request, CancellationToken cancellationToken)
        {
            var orgId = await SubdealerOrgService.ResolveOrgIdAsync(_unitOfWork, request.SubdealerId);
            var orgUserIds = await SubdealerOrgService.GetOrgLoginUserIdsAsync(_unitOfWork, orgId);
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var userOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var balances = await _unitOfWork.AccountBalances.GetAllAsync();

            var result = from a in accounts
                         join b in balances on a.AccountId equals b.SubdealerAccountId into balanceGroup
                         from balance in balanceGroup.DefaultIfEmpty()
                         where orgUserIds.Contains(a.SubdealerId)
                         select new SubdealerAccountDto
                         {
                             AccountId = a.AccountId,
                             SubdealerId = a.SubdealerId,
                             SubdealerName = SubdealerOrgService.ResolveDisplayName(a.SubdealerId, userOrgRoles, orgs, users),
                             AccountName = a.AccountName,
                             AccountType = a.AccountType,
                             Description = a.Description,
                             IsActive = a.IsActive,
                             CurrentBalance = balance != null ? balance.CurrentBalance : 0,
                             AvailableBalance = balance != null ? balance.AvailableBalance : 0,
                             ReservedAmount = balance != null ? balance.ReservedAmount : 0,
                             CreatedDate = a.CreatedDate,
                             ModifiedDate = a.ModifiedDate
                         };

            if (request.IsActive.HasValue)
                result = result.Where(a => a.IsActive == request.IsActive.Value);

            return result.OrderBy(a => a.AccountName).ToList();
        }
    }
}

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
            var wallet = await SubdealerOrgService.GetOrgWalletAccountAsync(_unitOfWork, orgId);
            if (wallet == null)
                return Array.Empty<SubdealerAccountDto>();

            if (request.IsActive.HasValue && wallet.IsActive != request.IsActive.Value)
                return Array.Empty<SubdealerAccountDto>();

            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var balances = await _unitOfWork.AccountBalances.GetAllAsync();
            var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == wallet.AccountId);

            return new[]
            {
                new SubdealerAccountDto
                {
                    AccountId = wallet.AccountId,
                    SubdealerId = orgId,
                    SubdealerName = SubdealerOrgService.ResolveOrgDisplayName(orgId, orgs),
                    AccountName = wallet.AccountName,
                    AccountType = wallet.AccountType,
                    Description = wallet.Description,
                    IsActive = wallet.IsActive,
                    CurrentBalance = balance?.CurrentBalance ?? 0,
                    AvailableBalance = balance?.AvailableBalance ?? 0,
                    ReservedAmount = balance?.ReservedAmount ?? 0,
                    CreatedDate = wallet.CreatedDate,
                    ModifiedDate = wallet.ModifiedDate
                }
            };
        }
    }
}

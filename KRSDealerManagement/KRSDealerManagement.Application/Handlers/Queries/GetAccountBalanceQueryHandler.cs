using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAccountBalanceQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AccountBalanceDto> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
        {
            var balances = await _unitOfWork.AccountBalances.GetAllAsync();
            var balance = balances.FirstOrDefault(b => b.SubdealerAccountId == request.SubdealerAccountId);

            if (balance == null)
                return new AccountBalanceDto
                {
                    BalanceId = 0,
                    SubdealerAccountId = request.SubdealerAccountId,
                    SubdealerId = 0,
                    SubdealerName = "Unknown",
                    AccountName = "Unknown",
                    CurrentBalance = 0,
                    ReservedAmount = 0,
                    AvailableBalance = 0
                };

            // Get account and user for display
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var account = accounts.FirstOrDefault(a => a.AccountId == balance.SubdealerAccountId);
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var orgId = await SubdealerOrgService.ResolveOrgIdFromWalletUserIdAsync(_unitOfWork, balance.SubdealerId);

            return new AccountBalanceDto
            {
                BalanceId = balance.BalanceId,
                SubdealerAccountId = balance.SubdealerAccountId,
                SubdealerId = orgId,
                SubdealerName = SubdealerOrgService.ResolveOrgDisplayName(orgId, orgs),
                AccountName = account?.AccountName ?? "Unknown",
                CurrentBalance = balance.CurrentBalance,
                ReservedAmount = balance.ReservedAmount,
                AvailableBalance = balance.AvailableBalance,
                InitialBalance = balance.InitialBalance,
                LastTransactionDate = balance.LastTransactionDate,
                CreatedDate = balance.CreatedDate,
                ModifiedDate = balance.ModifiedDate
            };
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Resolves subdealer accounts: wallet (shared org balance) and permission account (per login).
    /// </summary>
    public static class AccountHelper
    {
        /// <summary>Org wallet used for orders, payments, and statements.</summary>
        public static async Task<SubdealerAccountDto?> GetWalletAccountAsync(IMediator mediator, int subdealerOrgOrUserId)
        {
            var detail = await mediator.Send(new GetSubdealerDetailQuery { SubDealerId = subdealerOrgOrUserId });
            if (detail == null)
                detail = await mediator.Send(new GetSubdealerDetailQuery { UserId = subdealerOrgOrUserId });

            var walletOrgId = detail?.SubDealerId ?? subdealerOrgOrUserId;
            var accounts = await mediator.Send(new GetSubdealerAccountsQuery
            {
                SubdealerId = walletOrgId,
                IsActive = true
            });

            var list = accounts?.ToList() ?? new List<SubdealerAccountDto>();
            if (list.Count == 0) return null;

            return list.FirstOrDefault(a =>
                       string.Equals(a.AccountType, "Main", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a.AccountName, "Main Account", StringComparison.OrdinalIgnoreCase))
                   ?? list.First();
        }

        /// <summary>Backward-compatible alias — returns org wallet account.</summary>
        public static Task<SubdealerAccountDto?> GetPrimaryAccountAsync(IMediator mediator, int subdealerOrgOrUserId)
            => GetWalletAccountAsync(mediator, subdealerOrgOrUserId);
    }
}

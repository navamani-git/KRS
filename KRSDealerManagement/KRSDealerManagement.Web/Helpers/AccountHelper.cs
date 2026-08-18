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
        public static async Task<SubdealerAccountDto?> GetWalletAccountAsync(IMediator mediator, int loginUserId)
        {
            var primaryUserId = await ResolveWalletUserIdAsync(mediator, loginUserId);
            if (!primaryUserId.HasValue) return null;

            var accounts = await mediator.Send(new GetSubdealerAccountsQuery
            {
                SubdealerId = primaryUserId.Value,
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
        public static Task<SubdealerAccountDto?> GetPrimaryAccountAsync(IMediator mediator, int loginUserId)
            => GetWalletAccountAsync(mediator, loginUserId);

        private static async Task<int?> ResolveWalletUserIdAsync(IMediator mediator, int loginUserId)
        {
            var detail = await mediator.Send(new GetSubdealerDetailQuery { UserId = loginUserId });
            return detail?.PrimaryUserId ?? loginUserId;
        }
    }
}

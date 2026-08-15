using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;

namespace KRSDealerManagement.Web.Helpers
{
    /// <summary>
    /// Resolves the single primary account for a subdealer (one balance per subdealer).
    /// </summary>
    public static class AccountHelper
    {
        public static async Task<SubdealerAccountDto?> GetPrimaryAccountAsync(IMediator mediator, int subdealerId)
        {
            var accounts = await mediator.Send(new GetSubdealerAccountsQuery
            {
                SubdealerId = subdealerId,
                IsActive = true
            });

            var list = accounts?.ToList() ?? new List<SubdealerAccountDto>();
            if (list.Count == 0) return null;

            // Prefer Main account when present; otherwise first active account
            return list.FirstOrDefault(a =>
                       string.Equals(a.AccountType, "Main", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(a.AccountName, "Main Account", StringComparison.OrdinalIgnoreCase))
                   ?? list.First();
        }
    }
}

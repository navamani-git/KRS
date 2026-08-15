using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get account balance with current, reserved, and available amounts
    /// </summary>
    public class GetAccountBalanceQuery : IRequest<AccountBalanceDto>
    {
        public int SubdealerAccountId { get; set; }
    }
}

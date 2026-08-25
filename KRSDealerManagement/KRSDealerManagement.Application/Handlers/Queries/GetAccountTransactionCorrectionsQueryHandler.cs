using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetAccountTransactionCorrectionsQueryHandler
        : IRequestHandler<GetAccountTransactionCorrectionsQuery, IEnumerable<AccountTransactionCorrectionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAccountTransactionCorrectionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AccountTransactionCorrectionDto>> Handle(
            GetAccountTransactionCorrectionsQuery request,
            CancellationToken cancellationToken)
        {
            var corrections = (await _unitOfWork.AccountTransactionCorrections.GetAllAsync()).AsEnumerable();
            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToDictionary(a => a.AccountId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);

            if (request.AccountId.HasValue)
                corrections = corrections.Where(c => c.AccountId == request.AccountId.Value);
            if (request.TransactionId.HasValue)
                corrections = corrections.Where(c => c.TransactionId == request.TransactionId.Value);
            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                corrections = corrections.Where(c => c.CreatedDate >= from);
            }
            if (request.ToDate.HasValue)
            {
                var toExclusive = request.ToDate.Value.Date.AddDays(1);
                corrections = corrections.Where(c => c.CreatedDate < toExclusive);
            }

            return corrections
                .OrderByDescending(c => c.CreatedDate)
                .Select(c =>
                {
                    accounts.TryGetValue(c.AccountId, out var account);
                    string? subdealerName = null;
                    if (account != null && users.TryGetValue(account.SubdealerId, out var user))
                        subdealerName = user.GetFullName();

                    return new AccountTransactionCorrectionDto
                    {
                        CorrectionId = c.CorrectionId,
                        TransactionId = c.TransactionId,
                        AccountId = c.AccountId,
                        SubdealerName = subdealerName ?? $"Account #{c.AccountId}",
                        Action = c.Action,
                        OldSnapshot = c.OldSnapshot,
                        NewSnapshot = c.NewSnapshot,
                        CorrectionReason = c.CorrectionReason,
                        CorrectedBy = c.CorrectedBy,
                        CorrectedByName = c.CorrectedByName
                            ?? (users.TryGetValue(c.CorrectedBy, out var admin) ? admin.GetFullName() : $"User #{c.CorrectedBy}"),
                        CreatedDate = c.CreatedDate
                    };
                })
                .ToList();
        }
    }
}

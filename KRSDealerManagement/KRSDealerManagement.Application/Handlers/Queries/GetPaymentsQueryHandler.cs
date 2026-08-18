using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, IEnumerable<PaymentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetPaymentsQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
        {
            var payments = await _unitOfWork.Payments.GetAllAsync();
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var users = await _unitOfWork.Users.GetAllAsync();
            var financeNames = (await _unitOfWork.FinanceNames.GetAllAsync()).ToDictionary(f => f.FinanceNameId);
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Payment);

            var result = from p in payments
                         join a in accounts on p.AccountId equals a.AccountId into accGroup
                         from acc in accGroup.DefaultIfEmpty()
                         join u in users on p.SubdealerId equals u.UserId into userGroup
                         from user in userGroup.DefaultIfEmpty()
                         join pu in users on p.ProcessedBy equals pu.UserId into processedGroup
                         from processedBy in processedGroup.DefaultIfEmpty()
                         select new PaymentDto
                         {
                             PaymentId = p.PaymentId,
                             AccountId = p.AccountId,
                             AccountName = acc != null ? acc.AccountName : "Unknown",
                             SubdealerId = p.SubdealerId,
                             SubdealerName = user != null ? user.GetFullName() : "Unknown",
                             Amount = p.Amount,
                             ActualReceivedAmount = p.ActualReceivedAmount,
                             ActualReceivedDate = p.ActualReceivedDate,
                             PaymentType = p.PaymentType,
                             PaymentTypeId = p.PaymentTypeId,
                             CustomerName = p.CustomerName,
                             FinanceNameId = p.FinanceNameId,
                             FinanceName = p.FinanceNameId.HasValue && financeNames.TryGetValue(p.FinanceNameId.Value, out var fn)
                                 ? fn.FinanceName : null,
                             VinNumber = p.VinNumber,
                             PaymentProofPath = p.PaymentProofPath,
                             PaymentProof2Path = p.PaymentProof2Path,
                             PaymentDate = p.PaymentDate,
                             Status = p.Status,
                             StatusName = statusMap.TryGetValue(p.Status, out var st) ? st.StatusName : null,
                             StatusBadgeClass = statusMap.TryGetValue(p.Status, out st) ? st.BadgeClass : null,
                             SubdealerRemarks = p.SubdealerRemarks,
                             DealerRemarks = p.DealerRemarks,
                             ProcessedBy = p.ProcessedBy,
                             ProcessedByName = processedBy != null ? processedBy.GetFullName() : null,
                             ProcessedDate = p.ProcessedDate,
                             IsApplied = p.IsApplied,
                             TransactionId = p.TransactionId,
                             CreatedDate = p.CreatedDate,
                             ModifiedDate = p.ModifiedDate
                         };

            if (request.SubdealerId.HasValue)
                result = result.Where(p => p.SubdealerId == request.SubdealerId.Value);

            if (request.AccountId.HasValue)
                result = result.Where(p => p.AccountId == request.AccountId.Value);

            if (request.Status.HasValue)
                result = result.Where(p => p.Status == request.Status.Value);

            if (request.AppliedOnly == true)
                result = result.Where(p => p.IsApplied);

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                result = result.Where(p => p.PaymentDate.Date >= from);
            }

            if (request.ToDate.HasValue)
            {
                var to = request.ToDate.Value.Date;
                result = result.Where(p => p.PaymentDate.Date <= to);
            }

            return result.OrderByDescending(p => p.CreatedDate).ToList();
        }
    }
}

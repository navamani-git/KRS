using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetAccountTransactionsQueryHandler : IRequestHandler<GetAccountTransactionsQuery, IEnumerable<AccountTransactionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAccountTransactionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AccountTransactionDto>> Handle(GetAccountTransactionsQuery request, CancellationToken cancellationToken)
        {
            var transactions = await _unitOfWork.AccountTransactions.GetAllAsync();
            var filtered = transactions.Where(t => t.AccountId == request.AccountId);

            if (request.TransactionType.HasValue)
                filtered = filtered.Where(t => t.TransactionType == request.TransactionType.Value);

            if (!string.IsNullOrWhiteSpace(request.ReferenceType))
                filtered = filtered.Where(t => t.ReferenceType == request.ReferenceType);

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                filtered = filtered.Where(t => t.CreatedDate >= from);
            }

            if (request.ToDate.HasValue)
            {
                var toExclusive = request.ToDate.Value.Date.AddDays(1);
                filtered = filtered.Where(t => t.CreatedDate < toExclusive);
            }

            if (request.ExcludeBalanceHolds)
                filtered = filtered.Where(t => !AccountTransactionTypeHelper.IsBalanceHold(t.TransactionType));

            var commissions = (await _unitOfWork.Commissions.GetAllAsync()).ToDictionary(c => c.CommissionId);
            var returns = (await _unitOfWork.ReturnRequests.GetAllAsync()).ToDictionary(r => r.ReturnRequestId);
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var payments = (await _unitOfWork.Payments.GetAllAsync()).ToDictionary(p => p.PaymentId);
            var paymentTypes = (await _unitOfWork.PaymentTypes.GetAllAsync()).ToDictionary(pt => pt.PaymentTypeId);

            return filtered.OrderByDescending(t => t.CreatedDate)
                .Select(t =>
                {
                    var chassis = ResolveChassis(t.ReferenceType, t.ReferenceId, commissions, returns, vehicles);
                    var reason = EnrichReason(t.Reason, t.ReferenceType, chassis);
                    var category = AccountStatementCategoryHelper.Resolve(
                        t.TransactionType, t.ReferenceType, t.ReferenceId, t.Reason, payments, paymentTypes);

                    return new AccountTransactionDto
                    {
                        TransactionId = t.TransactionId,
                        AccountId = t.AccountId,
                        TransactionType = t.TransactionType,
                        Amount = t.Amount,
                        BalanceAfterTransaction = t.BalanceAfterTransaction,
                        Reason = reason,
                        ReferenceId = t.ReferenceId,
                        ReferenceType = t.ReferenceType ?? "",
                        CategoryLabel = category,
                        ChassisNumber = chassis,
                        Remarks = t.Remarks,
                        InitiatedBy = t.InitiatedBy,
                        InitiatedByName = $"User #{t.InitiatedBy}",
                        CreatedDate = t.CreatedDate
                    };
                }).ToList();
        }

        private static string? ResolveChassis(
            string? referenceType,
            int? referenceId,
            Dictionary<int, Domain.Entities.Commission> commissions,
            Dictionary<int, Domain.Entities.ReturnRequest> returns,
            Dictionary<int, Domain.Entities.Vehicle> vehicles)
        {
            if (!referenceId.HasValue || string.IsNullOrWhiteSpace(referenceType))
                return null;

            int? vehicleId = referenceType switch
            {
                "Commission" when commissions.TryGetValue(referenceId.Value, out var c) => c.VehicleId,
                "ReturnRequest" when returns.TryGetValue(referenceId.Value, out var r) => r.VehicleId,
                "Vehicle" => referenceId.Value,
                _ => null
            };

            if (!vehicleId.HasValue || !vehicles.TryGetValue(vehicleId.Value, out var vehicle))
                return null;

            return TransactionReasonHelper.FormatChassis(vehicle.ChassisNumber);
        }

        private static string EnrichReason(string? reason, string? referenceType, string? chassis)
        {
            if (string.IsNullOrWhiteSpace(chassis))
                return reason ?? "";

            return referenceType switch
            {
                "Commission" => TransactionReasonHelper.Commission(chassis),
                "ReturnRequest" => TransactionReasonHelper.Return(chassis),
                _ => reason ?? ""
            };
        }
    }
}

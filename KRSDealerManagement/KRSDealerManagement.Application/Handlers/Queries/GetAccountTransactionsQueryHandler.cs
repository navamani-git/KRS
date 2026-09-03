using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Helpers;
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

            if (!request.IncludeDeleted)
                filtered = filtered.Where(t => !t.IsDeleted);

            var commissions = (await _unitOfWork.Commissions.GetAllAsync()).ToDictionary(c => c.CommissionId);
            var returns = (await _unitOfWork.ReturnRequests.GetAllAsync()).ToDictionary(r => r.ReturnRequestId);
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var paymentsList = (await _unitOfWork.Payments.GetAllAsync()).ToList();
            var payments = paymentsList.ToDictionary(p => p.PaymentId);
            var paymentsByTransactionId = paymentsList
                .Where(p => p.TransactionId.HasValue)
                .GroupBy(p => p.TransactionId!.Value)
                .ToDictionary(g => g.Key, g => g.First());
            var paymentTypes = (await _unitOfWork.PaymentTypes.GetAllAsync()).ToDictionary(pt => pt.PaymentTypeId);
            var financeNames = (await _unitOfWork.FinanceNames.GetAllAsync()).ToDictionary(f => f.FinanceNameId);

            return filtered.OrderByDescending(t => t.CreatedDate)
                .Select(t =>
                {
                    var chassis = ResolveChassis(t.ReferenceType, t.ReferenceId, commissions, returns, vehicles);
                    string reason;

                    if (string.Equals(t.ReferenceType, "Vehicle", StringComparison.OrdinalIgnoreCase)
                        && t.ReferenceId.HasValue
                        && vehicles.TryGetValue(t.ReferenceId.Value, out var vehicle))
                    {
                        models.TryGetValue(vehicle.ModelId, out var model);
                        colors.TryGetValue(vehicle.ColorId, out var color);
                        var formattedChassis = TransactionReasonHelper.FormatChassis(vehicle.ChassisNumber)
                            ?? vehicle.ChassisNumber
                            ?? "-";
                        chassis = formattedChassis;
                        reason = AccountStatementDescriptionHelper.FormatVehicle(
                            formattedChassis,
                            model?.ModelName ?? "Unknown",
                            color?.ColorName ?? "Unknown");
                    }
                    else
                    {
                        reason = AccountStatementDescriptionHelper.NormalizeOrderVehicleReason(t.Reason);
                        reason = EnrichReason(reason, t.ReferenceType, t.ReferenceId, chassis, returns, vehicles, models, colors);
                    }

                    string? customerName = null;
                    string? paymentType = null;
                    int? paymentTypeId = null;
                    string? financeName = null;
                    int? financeNameId = null;
                    string? vinNumber = null;
                    decimal? requestedAmount = null;
                    decimal? approvedPaymentAmount = null;
                    decimal? actualReceivedAmount = null;
                    DateTime? submittedDate = null;
                    DateTime? approvedDate = null;
                    DateTime? receivedDate = null;
                    var referenceType = t.ReferenceType ?? "";
                    var referenceId = t.ReferenceId;

                    var pay = PaymentStatementResolver.Resolve(t, payments, paymentsByTransactionId);
                    if (pay != null && AccountTransactionTypeHelper.IsCredit(t.TransactionType))
                    {
                        referenceType = "Payment";
                        referenceId = pay.PaymentId;
                        customerName = pay.CustomerName;
                        paymentType = pay.PaymentType;
                        paymentTypeId = pay.PaymentTypeId;
                        vinNumber = pay.VinNumber;
                        requestedAmount = pay.Amount;
                        approvedPaymentAmount = pay.ActualReceivedAmount ?? t.Amount;
                        actualReceivedAmount = approvedPaymentAmount;
                        submittedDate = pay.CreatedDate;
                        approvedDate = pay.ProcessedDate;
                        receivedDate = pay.ActualReceivedDate;
                        if (pay.FinanceNameId.HasValue && financeNames.TryGetValue(pay.FinanceNameId.Value, out var fn))
                        {
                            financeName = fn.FinanceName;
                            financeNameId = pay.FinanceNameId;
                        }
                    }

                    var category = AccountStatementCategoryHelper.Resolve(
                        t.TransactionType, referenceType, referenceId, t.Reason, payments, paymentTypes);

                    return new AccountTransactionDto
                    {
                        TransactionId = t.TransactionId,
                        AccountId = t.AccountId,
                        TransactionType = t.TransactionType,
                        Amount = t.Amount,
                        BalanceAfterTransaction = t.BalanceAfterTransaction,
                        Reason = reason,
                        ReferenceId = referenceId,
                        ReferenceType = referenceType,
                        CategoryLabel = category,
                        ChassisNumber = chassis,
                        Remarks = t.Remarks,
                        InitiatedBy = t.InitiatedBy,
                        InitiatedByName = $"User #{t.InitiatedBy}",
                        CreatedDate = t.CreatedDate,
                        CustomerName = customerName,
                        PaymentType = paymentType,
                        PaymentTypeId = paymentTypeId,
                        FinanceName = financeName,
                        FinanceNameId = financeNameId,
                        VinNumber = vinNumber,
                        RequestedAmount = requestedAmount,
                        ApprovedPaymentAmount = approvedPaymentAmount,
                        ActualReceivedAmount = actualReceivedAmount,
                        PaymentSubmittedDate = submittedDate,
                        PaymentApprovedDate = approvedDate,
                        PaymentReceivedDate = receivedDate
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

        private static string EnrichReason(
            string? reason,
            string? referenceType,
            int? referenceId,
            string? chassis,
            Dictionary<int, Domain.Entities.ReturnRequest> returns,
            Dictionary<int, Domain.Entities.Vehicle> vehicles,
            Dictionary<int, Domain.Entities.VehicleModel> models,
            Dictionary<int, Domain.Entities.VehicleColor> colors)
        {
            if (string.Equals(referenceType, "ReturnRequest", StringComparison.OrdinalIgnoreCase)
                && referenceId.HasValue
                && returns.TryGetValue(referenceId.Value, out var returnRequest)
                && vehicles.TryGetValue(returnRequest.VehicleId, out var vehicle))
            {
                models.TryGetValue(vehicle.ModelId, out var model);
                colors.TryGetValue(vehicle.ColorId, out var color);
                return TransactionReasonHelper.Return(
                    chassis ?? vehicle.ChassisNumber,
                    model?.ModelName,
                    color?.ColorName);
            }

            if (string.IsNullOrWhiteSpace(chassis))
                return reason ?? "";

            return referenceType switch
            {
                "Commission" => TransactionReasonHelper.Commission(chassis),
                _ => reason ?? ""
            };
        }
    }
}

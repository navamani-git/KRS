using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CarryForwardCommissionRatesCommandHandler : IRequestHandler<CarryForwardCommissionRatesCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommissionRateService _commissionRates;
        private readonly IAuditService _auditService;

        public CarryForwardCommissionRatesCommandHandler(
            IUnitOfWork unitOfWork,
            ICommissionRateService commissionRates,
            IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _commissionRates = commissionRates;
            _auditService = auditService;
        }

        public async Task<int> Handle(CarryForwardCommissionRatesCommand request, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var currentStart = new DateTime(today.Year, today.Month, 1);
            var currentEnd = currentStart.AddMonths(1).AddDays(-1);
            var previousEnd = currentStart.AddDays(-1);
            var prevStart = currentStart.AddMonths(-1);

            var allRates = (await _unitOfWork.CommissionRates.GetAllAsync()).ToList();
            var modelIds = allRates
                .Where(r => r.EffectiveFrom <= previousEnd && r.EffectiveTo >= prevStart)
                .Select(r => r.ModelId)
                .Distinct()
                .ToList();

            var created = 0;

            foreach (var modelId in modelIds)
            {
                var hasCurrentMonth = allRates.Any(r =>
                    r.ModelId == modelId
                    && r.EffectiveFrom <= currentEnd
                    && r.EffectiveTo >= currentStart);

                if (hasCurrentMonth)
                    continue;

                var sourceRate = await _commissionRates.GetRateAsOfAsync(modelId, previousEnd);
                if (sourceRate == null)
                    continue;

                if (allRates.Any(r => r.ModelId == modelId
                        && CommissionRateOverlapHelper.RangesOverlap(
                            currentStart, currentEnd, r.EffectiveFrom, r.EffectiveTo)))
                    continue;

                var rate = new CommissionRate
                {
                    ModelId = modelId,
                    CommissionAmount = sourceRate.CommissionAmount,
                    EffectiveFrom = currentStart,
                    EffectiveTo = currentEnd,
                    StartMonth = currentStart.Month,
                    StartYear = currentStart.Year,
                    ExpiryMonth = currentEnd.Month,
                    ExpiryYear = currentEnd.Year,
                    Notes = $"Carried forward from {previousEnd:yyyy-MM}",
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                var id = await _unitOfWork.CommissionRates.AddAsync(rate);
                rate.CommissionRateId = id;
                allRates.Add(rate);
                created++;

                await _auditService.LogActionAsync(
                    entityType: "CommissionRate",
                    entityId: id,
                    action: "CarryForward",
                    userId: request.CreatedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        modelId,
                        From = currentStart.ToString("yyyy-MM-dd"),
                        To = currentEnd.ToString("yyyy-MM-dd"),
                        Amount = sourceRate.CommissionAmount
                    }));
            }

            if (created > 0)
                await _unitOfWork.SaveChangesAsync();

            return created;
        }
    }
}

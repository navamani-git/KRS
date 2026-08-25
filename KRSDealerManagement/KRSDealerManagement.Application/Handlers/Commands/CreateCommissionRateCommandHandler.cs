using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class CreateCommissionRateCommandHandler : IRequestHandler<CreateCommissionRateCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CreateCommissionRateCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> Handle(CreateCommissionRateCommand request, CancellationToken cancellationToken)
        {
            var effectiveFrom = request.EffectiveFrom.Date;
            var effectiveTo = request.EffectiveTo.Date;

            if (effectiveTo < effectiveFrom)
                throw new InvalidOperationException("Effective to must be on or after effective from.");

            var existing = (await _unitOfWork.CommissionRates.GetAllAsync()).ToList();

            if (CommissionRateOverlapHelper.TryFindOverlap(
                    existing, request.ModelId, effectiveFrom, effectiveTo, excludeRateId: null, out var conflict)
                && conflict != null)
            {
                var (otherFrom, otherTo) = CommissionRateOverlapHelper.NormalizeRange(conflict);
                throw new InvalidOperationException(
                    CommissionRateOverlapHelper.OverlapMessage(effectiveFrom, effectiveTo, otherFrom, otherTo));
            }

            var rate = new CommissionRate
            {
                ModelId = request.ModelId,
                CommissionAmount = request.CommissionAmount,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                StartMonth = effectiveFrom.Month,
                StartYear = effectiveFrom.Year,
                ExpiryMonth = effectiveTo.Month,
                ExpiryYear = effectiveTo.Year,
                Notes = request.Notes,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var rateId = await _unitOfWork.CommissionRates.AddAsync(rate);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "CommissionRate",
                entityId: rateId,
                action: "Create",
                userId: request.CreatedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    ModelId = request.ModelId,
                    Amount = request.CommissionAmount,
                    From = effectiveFrom.ToString("yyyy-MM-dd"),
                    To = effectiveTo.ToString("yyyy-MM-dd")
                })
            );

            return rateId;
        }
    }
}

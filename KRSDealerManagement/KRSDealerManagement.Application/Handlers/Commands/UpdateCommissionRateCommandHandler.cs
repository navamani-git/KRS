using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateCommissionRateCommandHandler : IRequestHandler<UpdateCommissionRateCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public UpdateCommissionRateCommandHandler(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<bool> Handle(UpdateCommissionRateCommand request, CancellationToken cancellationToken)
        {
            var row = await _unitOfWork.CommissionRates.GetByIdAsync(request.CommissionRateId);
            if (row == null) return false;

            var effectiveFrom = request.EffectiveFrom.Date;
            var effectiveTo = request.EffectiveTo.Date;

            var existing = (await _unitOfWork.CommissionRates.GetAllAsync()).ToList();

            if (CommissionRateOverlapHelper.TryFindOverlap(
                    existing, row.ModelId, effectiveFrom, effectiveTo, row.CommissionRateId, out var conflict)
                && conflict != null)
            {
                var (otherFrom, otherTo) = CommissionRateOverlapHelper.NormalizeRange(conflict);
                throw new InvalidOperationException(
                    CommissionRateOverlapHelper.OverlapMessage(effectiveFrom, effectiveTo, otherFrom, otherTo));
            }

            row.CommissionAmount = request.CommissionAmount;
            row.EffectiveFrom = effectiveFrom;
            row.EffectiveTo = effectiveTo;
            row.StartMonth = effectiveFrom.Month;
            row.StartYear = effectiveFrom.Year;
            row.ExpiryMonth = effectiveTo.Month;
            row.ExpiryYear = effectiveTo.Year;
            row.Notes = request.Notes;
            row.ModifiedBy = request.ModifiedBy;
            row.ModifiedDate = DateTime.UtcNow;

            await _unitOfWork.CommissionRates.UpdateAsync(row);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "CommissionRate",
                entityId: row.CommissionRateId,
                action: "Update",
                userId: request.ModifiedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    Amount = row.CommissionAmount,
                    From = effectiveFrom.ToString("yyyy-MM-dd"),
                    To = effectiveTo.ToString("yyyy-MM-dd")
                }));

            return true;
        }
    }
}

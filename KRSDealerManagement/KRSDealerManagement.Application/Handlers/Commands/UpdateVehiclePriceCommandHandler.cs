using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class UpdateVehiclePriceCommandHandler : IRequestHandler<UpdateVehiclePriceCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IVehiclePriceService _priceService;

        public UpdateVehiclePriceCommandHandler(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IVehiclePriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _priceService = priceService;
        }

        public async Task<bool> Handle(UpdateVehiclePriceCommand request, CancellationToken cancellationToken)
        {
            var row = await _unitOfWork.VehiclePriceHistories.GetByIdAsync(request.PriceHistoryId);
            if (row == null) return false;

            var effectiveFrom = request.EffectiveFrom.Date;
            var effectiveTo = request.EffectiveTo.Date;
            if (effectiveTo < effectiveFrom)
                throw new InvalidOperationException("Effective to must be on or after effective from.");

            var existing = (await _unitOfWork.VehiclePriceHistories.GetAllAsync()).ToList();
            if (VehiclePriceOverlapHelper.TryFindOverlap(
                    existing, row.ModelId, row.ColorId, effectiveFrom, effectiveTo, row.PriceHistoryId, out var conflict)
                && conflict != null)
            {
                var (otherFrom, otherTo) = VehiclePriceOverlapHelper.NormalizeRange(conflict);
                throw new InvalidOperationException(
                    VehiclePriceOverlapHelper.OverlapMessage(effectiveFrom, effectiveTo, otherFrom, otherTo));
            }

            var oldPrice = row.Price;
            row.EffectiveFrom = effectiveFrom;
            row.EffectiveTo = effectiveTo;
            row.Month = effectiveFrom.Month;
            row.Year = effectiveFrom.Year;
            row.Price = request.Price;
            row.Notes = request.Notes;
            row.ModifiedBy = request.ModifiedBy;
            row.ModifiedDate = DateTime.UtcNow;

            await _unitOfWork.VehiclePriceHistories.UpdateAsync(row);
            await _unitOfWork.SaveChangesAsync();

            if (oldPrice != request.Price)
            {
                await _priceService.ApplyCatalogPriceRevisionAsync(
                    row.ModelId, row.ColorId, request.Price, effectiveFrom, request.ModifiedBy);
            }

            await _auditService.LogActionAsync(
                entityType: "VehiclePrice",
                entityId: row.PriceHistoryId,
                action: "Update",
                userId: request.ModifiedBy,
                userRole: "Admin",
                newValue: JsonSerializer.Serialize(new
                {
                    row.ModelId,
                    row.ColorId,
                    From = effectiveFrom.ToString("yyyy-MM-dd"),
                    To = effectiveTo.ToString("yyyy-MM-dd"),
                    request.Price,
                    request.Remarks
                }));

            return true;
        }
    }
}

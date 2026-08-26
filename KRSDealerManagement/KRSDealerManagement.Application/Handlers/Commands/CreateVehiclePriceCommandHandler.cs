using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Creates catalogue price for Model + Color with effective date range.
    /// Supports apply-for-all-colors and overlap validation.
    /// </summary>
    public class CreateVehiclePriceCommandHandler : IRequestHandler<CreateVehiclePriceCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IVehiclePriceService _priceService;

        public CreateVehiclePriceCommandHandler(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IVehiclePriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _priceService = priceService;
        }

        public async Task<int> Handle(CreateVehiclePriceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var model = await _unitOfWork.VehicleModels.GetByIdAsync(request.ModelId);
                if (model == null)
                    throw new InvalidOperationException($"Vehicle model #{request.ModelId} not found.");

                var colorIds = await ResolveColorIdsAsync(request);
                if (colorIds.Count == 0)
                    throw new InvalidOperationException("No colors selected for this model.");

                var effectiveFrom = request.EffectiveFrom == default
                    ? new DateTime(request.Year, request.Month, 1)
                    : request.EffectiveFrom.Date;

                var effectiveTo = request.EffectiveTo == default
                    ? new DateTime(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month))
                    : request.EffectiveTo.Date;

                if (effectiveTo < effectiveFrom)
                    throw new InvalidOperationException("Effective to must be on or after effective from.");

                var existing = (await _unitOfWork.VehiclePriceHistories.GetAllAsync()).ToList();
                var firstPriceId = 0;

                foreach (var colorId in colorIds)
                {
                    var color = await _unitOfWork.VehicleColors.GetByIdAsync(colorId);
                    if (color == null)
                        throw new InvalidOperationException($"Vehicle color #{colorId} not found.");

                    await ModelColorValidation.EnsureMappedAsync(_unitOfWork, request.ModelId, colorId);

                    if (VehiclePriceOverlapHelper.TryFindOverlap(
                            existing, request.ModelId, colorId, effectiveFrom, effectiveTo, excludePriceHistoryId: null, out var conflict)
                        && conflict != null)
                    {
                        var (otherFrom, otherTo) = VehiclePriceOverlapHelper.NormalizeRange(conflict);
                        throw new InvalidOperationException(
                            VehiclePriceOverlapHelper.OverlapMessage(effectiveFrom, effectiveTo, otherFrom, otherTo));
                    }

                    var priceHistory = new VehiclePriceHistory
                    {
                        ModelId = request.ModelId,
                        ColorId = colorId,
                        VehicleId = null,
                        Month = effectiveFrom.Month,
                        Year = effectiveFrom.Year,
                        EffectiveFrom = effectiveFrom,
                        EffectiveTo = effectiveTo,
                        Price = request.Price,
                        Notes = request.Notes,
                        CreatedBy = request.CreatedBy,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };

                    var priceId = await _unitOfWork.VehiclePriceHistories.AddAsync(priceHistory);
                    if (firstPriceId == 0)
                        firstPriceId = priceId;

                    priceHistory.PriceHistoryId = priceId;
                    existing.Add(priceHistory);

                    await _priceService.ApplyCatalogPriceRevisionAsync(
                        request.ModelId, colorId, request.Price, effectiveFrom, request.CreatedBy);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _auditService.LogActionAsync(
                    entityType: "VehiclePrice",
                    entityId: firstPriceId,
                    action: "Create",
                    userId: request.CreatedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        request.ModelId,
                        ColorCount = colorIds.Count,
                        request.ApplyForAllColors,
                        effectiveFrom,
                        effectiveTo,
                        request.Price
                    }));

                return firstPriceId;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error creating vehicle price: {ex.Message}", ex);
            }
        }

        private async Task<List<int>> ResolveColorIdsAsync(CreateVehiclePriceCommand request)
        {
            if (request.ColorIds is { Count: > 0 })
                return request.ColorIds.Distinct().ToList();

            if (request.ApplyForAllColors)
                return (await _unitOfWork.VehicleModelColors.GetColorIdsByModelIdAsync(request.ModelId)).ToList();

            if (request.ColorId <= 0)
                throw new InvalidOperationException("Select at least one color.");

            return new List<int> { request.ColorId };
        }
    }
}

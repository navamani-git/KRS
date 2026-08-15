using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    /// <summary>
    /// Creates catalogue price for Model + Color with effective-from date.
    /// Multiple entries per month are allowed; revises allocated/invoiced vehicles on/after effective date.
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

                var color = await _unitOfWork.VehicleColors.GetByIdAsync(request.ColorId);
                if (color == null)
                    throw new InvalidOperationException($"Vehicle color #{request.ColorId} not found.");

                var effectiveFrom = request.EffectiveFrom == default
                    ? new DateTime(request.Year, request.Month, 1)
                    : request.EffectiveFrom.Date;

                var priceHistory = new VehiclePriceHistory
                {
                    ModelId = request.ModelId,
                    ColorId = request.ColorId,
                    VehicleId = null,
                    Month = effectiveFrom.Month,
                    Year = effectiveFrom.Year,
                    EffectiveFrom = effectiveFrom,
                    Price = request.Price,
                    Notes = request.Notes,
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                var priceId = await _unitOfWork.VehiclePriceHistories.AddAsync(priceHistory);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _priceService.ApplyCatalogPriceRevisionAsync(
                    request.ModelId, request.ColorId, request.Price, effectiveFrom, request.CreatedBy);

                await _auditService.LogActionAsync(
                    entityType: "VehiclePrice",
                    entityId: priceId,
                    action: "Create",
                    userId: request.CreatedBy,
                    userRole: "Admin",
                    newValue: JsonSerializer.Serialize(new
                    {
                        request.ModelId,
                        request.ColorId,
                        effectiveFrom,
                        request.Price
                    }));

                return priceId;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new ApplicationException($"Error creating vehicle price: {ex.Message}", ex);
            }
        }
    }
}

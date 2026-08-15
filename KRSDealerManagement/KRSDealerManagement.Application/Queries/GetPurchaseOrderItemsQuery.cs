using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Application.Queries
{
    public class GetPurchaseOrderItemsQuery : IRequest<IEnumerable<PurchaseOrderItemDto>>
    {
        public int OrderId { get; set; }
    }

    public class GetPurchaseOrderItemsQueryHandler : IRequestHandler<GetPurchaseOrderItemsQuery, IEnumerable<PurchaseOrderItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetPurchaseOrderItemsQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<PurchaseOrderItemDto>> Handle(GetPurchaseOrderItemsQuery request, CancellationToken cancellationToken)
        {
            var items = await _unitOfWork.PurchaseOrderItems.GetByOrderIdAsync(request.OrderId);
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);

            return items.Select(i =>
            {
                var vehicleStatus = i.VehicleId.HasValue && vehicles.TryGetValue(i.VehicleId.Value, out var v)
                    ? v.Status
                    : (i.Status == 2 ? UnifiedVehicleStatus.RejectedByDealer : UnifiedVehicleStatus.Submitted);

                statusMap.TryGetValue(vehicleStatus, out var st);

                return new PurchaseOrderItemDto
                {
                    OrderItemId = i.OrderItemId,
                    PurchaseOrderId = i.PurchaseOrderId,
                    ModelId = i.ModelId,
                    ModelName = models.TryGetValue(i.ModelId, out var m) ? m.ModelName : $"Model #{i.ModelId}",
                    ColorId = i.ColorId,
                    ColorName = colors.TryGetValue(i.ColorId, out var c) ? c.ColorName : $"Color #{i.ColorId}",
                    UnitPrice = i.UnitPrice,
                    Status = i.Status,
                    VehicleStatus = vehicleStatus,
                    StatusName = st?.StatusName,
                    StatusBadgeClass = st?.BadgeClass,
                    MotorNo = i.MotorNo,
                    BatteryNo = i.BatteryNo,
                    ChargerNo = i.ChargerNo,
                    ControllerNo = i.ControllerNo,
                    ConverterNo = i.ConverterNo,
                    ChassisNumber = i.ChassisNumber,
                    VehicleId = i.VehicleId,
                    Remarks = i.Remarks,
                    CreatedDate = i.CreatedDate,
                    ApprovedDate = i.ApprovedDate,
                    RejectedDate = i.RejectedDate
                };
            }).ToList();
        }
    }
}

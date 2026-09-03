using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, IEnumerable<VehicleDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetVehiclesQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IEnumerable<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
        {
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var orders = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToDictionary(o => o.OrderId);
            var orderItemByVehicleId = (await _unitOfWork.PurchaseOrderItems.GetAllAsync())
                .Where(i => i.VehicleId.HasValue)
                .GroupBy(i => i.VehicleId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.ApprovedDate ?? i.CreatedDate).First());
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToDictionary(b => b.VehicleId);
            var pendingReturns = (await _unitOfWork.ReturnRequests.GetAllAsync())
                .Where(r => r.Status == 0)
                .Select(r => r.VehicleId)
                .ToHashSet();
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToList();
            var userOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();

            // Vehicles assigned to subdealers, plus dealer-showroom stock (no subdealer after return)
            var result = vehicles
                .Where(v =>
                    (v.SubdealerId.HasValue && v.SubdealerId.Value > 0)
                    || (!v.SubdealerId.HasValue && v.PurchaseOrderId.HasValue))
                .Select(v =>
                {
                    users.TryGetValue(v.SubdealerId ?? 0, out var user);
                    models.TryGetValue(v.ModelId, out var model);
                    colors.TryGetValue(v.ColorId, out var color);
                    orders.TryGetValue(v.PurchaseOrderId ?? 0, out var order);
                    orderItemByVehicleId.TryGetValue(v.VehicleId, out var orderItem);
                    statusMap.TryGetValue(v.Status, out var st);
                    bookings.TryGetValue(v.VehicleId, out var booking);

                    return new VehicleDto
                    {
                        VehicleId = v.VehicleId,
                        ModelId = v.ModelId,
                        ModelName = model?.ModelName ?? $"Model #{v.ModelId}",
                        ColorId = v.ColorId,
                        ColorName = color?.ColorName ?? $"Color #{v.ColorId}",
                        ChassisNumber = UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber) ? "-" : (v.ChassisNumber ?? "-"),
                        Status = v.Status,
                        StatusName = st?.StatusName,
                        StatusBadgeClass = st?.BadgeClass,
                        SubdealerId = v.SubdealerId,
                        SubdealerName = v.SubdealerId.HasValue
                            ? (user?.GetFullName() ?? "Unknown")
                            : "Dealer Showroom",
                        PurchaseOrderId = v.PurchaseOrderId,
                        OrderNumber = order?.OrderNumber,
                        OrderDate = order?.CreatedDate,
                        AllocatedDate = orderItem?.ApprovedDate,
                        CreatedByDealer = order?.CreatedByDealer ?? false,
                        CurrentPrice = v.CurrentPrice,
                        MotorNo = v.MotorNo,
                        BatteryNo = v.BatteryNo,
                        ChargerNo = v.ChargerNo,
                        ControllerNo = v.ControllerNo,
                        ConverterNo = v.ConverterNo,
                        ManufacturingYear = v.ManufacturingYear,
                        RegistrationNumber = v.RegistrationNumber,
                        StockLocation = v.StockLocation,
                        Notes = v.Notes,
                        CreatedBy = v.CreatedBy,
                        CreatedDate = v.CreatedDate,
                        ModifiedBy = v.ModifiedBy,
                        ModifiedDate = v.ModifiedDate,
                        DeliveryDate = v.DeliveryDate,
                        VehicleBookingId = booking?.VehicleBookingId,
                        BookingInvoiceDate = booking?.InvoiceDate,
                        BookingInsuranceDate = booking?.InsuranceDate,
                        InvoicePath = booking?.InvoicePath,
                        InsurancePath = booking?.InsurancePath,
                        BookingStatus = v.Status,
                        BookingStatusName = st?.StatusName,
                        BookingStatusBadge = st?.BadgeClass,
                        CanSubmitSubsidyDocs = booking != null
                            && BookingStageFilter.IsSubsidyDocsPending(
                                booking.SubsidyId,
                                booking.FaceVerificationPath,
                                booking.RcImagePath,
                                booking.BoothPhotoPath,
                                booking.SubsidyUndertakingPath,
                                v.Status),
                        CanRequestReturn = !(order?.CreatedByDealer ?? false)
                            && UnifiedVehicleStatus.CanBookOrReturnPreInvoice(
                                v.Status, booking != null, booking?.InvoiceDate)
                            && UnifiedVehicleStatus.CanRequestReturn(v.Status)
                            && !pendingReturns.Contains(v.VehicleId)
                    };
                });

            if (request.SubdealerId.HasValue)
                result = result.Where(v => v.SubdealerId == request.SubdealerId.Value);

            if (request.DealershipId.HasValue)
            {
                var scopedUserIds = userOrgRoles
                    .Where(a => a.IsActive && a.DealershipId == request.DealershipId.Value)
                    .Select(a => a.UserId)
                    .ToHashSet();
                result = result.Where(v =>
                    (v.SubdealerId.HasValue && scopedUserIds.Contains(v.SubdealerId.Value))
                    || (!v.SubdealerId.HasValue
                        && v.PurchaseOrderId.HasValue
                        && orders.TryGetValue(v.PurchaseOrderId.Value, out var po)
                        && scopedUserIds.Contains(po.SubdealerId)));
            }

            if (!string.IsNullOrWhiteSpace(request.DealershipLocation))
            {
                var location = request.DealershipLocation.Trim();
                var dealershipIds = dealerships
                    .Where(d => d.IsActive
                        && string.Equals(d.Location?.Trim(), location, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.DealershipId)
                    .ToHashSet();
                var locationUserIds = userOrgRoles
                    .Where(a => a.IsActive && a.DealershipId.HasValue && dealershipIds.Contains(a.DealershipId.Value))
                    .Select(a => a.UserId)
                    .ToHashSet();
                result = result.Where(v => v.SubdealerId.HasValue && locationUserIds.Contains(v.SubdealerId.Value));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                result = result.Where(v =>
                    v.ChassisNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (v.OrderNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || v.SubdealerName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (v.MotorNo?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (request.FromDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                result = result.Where(v => v.CreatedDate >= from);
            }

            if (request.ToDate.HasValue)
            {
                var toExclusive = request.ToDate.Value.Date.AddDays(1);
                result = result.Where(v => v.CreatedDate < toExclusive);
            }

            if (request.RejectedOnly)
                result = result.Where(v => v.Status == UnifiedVehicleStatus.RejectedByDealer);
            else if (request.ExcludeRejected)
                result = result.Where(v => v.Status != UnifiedVehicleStatus.RejectedByDealer);

            if (request.ColumnFilters is { Count: > 0 } cf)
            {
                result = result.Where(v =>
                    GridFilterHelper.MatchesContains(v.SubdealerName, GridFilterHelper.GetFilter(cf, "subdealer"))
                    && GridFilterHelper.MatchesDate(v.OrderDate, GridFilterHelper.GetDateFilter(cf, "orderDate"), GridFilterHelper.GetDateFilter(cf, "orderDate"))
                    && GridFilterHelper.MatchesContains(v.OrderNumber, GridFilterHelper.GetFilter(cf, "orderNumber"))
                    && GridFilterHelper.MatchesDate(v.AllocatedDate, GridFilterHelper.GetDateFilter(cf, "allocated"), GridFilterHelper.GetDateFilter(cf, "allocated"))
                    && GridFilterHelper.MatchesContains(v.ChassisNumber, GridFilterHelper.GetFilter(cf, "chassis"))
                    && GridFilterHelper.MatchesContains(v.ModelName, GridFilterHelper.GetFilter(cf, "model"))
                    && GridFilterHelper.MatchesContains(v.ColorName, GridFilterHelper.GetFilter(cf, "color"))
                    && GridFilterHelper.MatchesExact(v.GetSourceDisplay(), GridFilterHelper.GetFilter(cf, "source"))
                    && GridFilterHelper.MatchesContains(v.MotorNo, GridFilterHelper.GetFilter(cf, "motor"))
                    && GridFilterHelper.MatchesContains(v.BatteryNo, GridFilterHelper.GetFilter(cf, "battery"))
                    && GridFilterHelper.MatchesContains(v.GetStatusDisplay(), GridFilterHelper.GetFilter(cf, "status"))
                    && GridFilterHelper.MatchesContains(v.GetDeliveryStatusDisplay(), GridFilterHelper.GetFilter(cf, "delivery"))
                    && GridFilterHelper.MatchesContains(v.CurrentPrice.ToString("N2"), GridFilterHelper.GetFilter(cf, "price")));
            }

            return result.OrderByDescending(v => v.CreatedDate).ToList();
        }
    }
}

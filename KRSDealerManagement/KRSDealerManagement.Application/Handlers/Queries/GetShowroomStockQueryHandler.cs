using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetShowroomStockQueryHandler : IRequestHandler<GetShowroomStockQuery, IEnumerable<ShowroomStockRowDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetShowroomStockQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ShowroomStockRowDto>> Handle(GetShowroomStockQuery request, CancellationToken cancellationToken)
        {
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var bookingsByVehicle = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .GroupBy(b => b.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var orders = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToDictionary(o => o.OrderId);
            var orderItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync())
                .Where(i => i.VehicleId.HasValue)
                .GroupBy(i => i.VehicleId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.ApprovedDate ?? i.CreatedDate).First());
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var orgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.IsActive)
                .GroupBy(a => a.UserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.IsPrimary).First());

            HashSet<int>? scopedSubdealerIds = null;
            if (request.DealershipId.HasValue)
            {
                scopedSubdealerIds = orgRoles.Values
                    .Where(a => a.DealershipId == request.DealershipId.Value)
                    .Select(a => a.UserId)
                    .ToHashSet();
            }

            if (!string.IsNullOrWhiteSpace(request.DealershipLocation))
            {
                var location = request.DealershipLocation.Trim();
                var locationDealershipIds = dealerships.Values
                    .Where(d => d.IsActive
                        && string.Equals(d.Location?.Trim(), location, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.DealershipId)
                    .ToHashSet();
                var locationSubdealerIds = orgRoles.Values
                    .Where(a => a.DealershipId.HasValue && locationDealershipIds.Contains(a.DealershipId.Value))
                    .Select(a => a.UserId)
                    .ToHashSet();
                scopedSubdealerIds = scopedSubdealerIds == null
                    ? locationSubdealerIds
                    : scopedSubdealerIds.Intersect(locationSubdealerIds).ToHashSet();
            }

            var rows = vehicles
                .Where(v =>
                {
                    bookingsByVehicle.TryGetValue(v.VehicleId, out var booking);
                    return ShowroomStockFilter.IsShowroomStock(
                        v.Status,
                        v.SubdealerId,
                        booking?.InvoiceDate,
                        booking != null);
                })
                .Select(v =>
                {
                    bookingsByVehicle.TryGetValue(v.VehicleId, out var booking);
                    users.TryGetValue(v.SubdealerId ?? 0, out var user);
                    models.TryGetValue(v.ModelId, out var model);
                    colors.TryGetValue(v.ColorId, out var color);
                    orders.TryGetValue(v.PurchaseOrderId ?? 0, out var order);
                    orderItems.TryGetValue(v.VehicleId, out var item);

                    string? location = null;
                    string? dealershipName = null;
                    if (v.SubdealerId.HasValue
                        && orgRoles.TryGetValue(v.SubdealerId.Value, out var org)
                        && org.DealershipId.HasValue
                        && dealerships.TryGetValue(org.DealershipId.Value, out var dealer))
                    {
                        location = dealer.Location?.Trim();
                        dealershipName = dealer.DealershipName;
                    }

                    var allocated = item?.ApprovedDate ?? order?.ApprovedDate ?? v.CreatedDate;
                    var chassis = UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber)
                        ? "-"
                        : (v.ChassisNumber ?? "-");

                    return new ShowroomStockRowDto
                    {
                        VehicleId = v.VehicleId,
                        ChassisNumber = chassis,
                        ModelName = model?.ModelName ?? $"Model #{v.ModelId}",
                        ColorName = color?.ColorName ?? $"Color #{v.ColorId}",
                        SubdealerId = v.SubdealerId!.Value,
                        SubdealerName = user?.GetFullName() ?? "Unknown",
                        DealershipLocation = location,
                        DealershipName = dealershipName,
                        OrderNumber = order?.OrderNumber,
                        AllocatedDate = allocated,
                        CurrentPrice = v.CurrentPrice,
                        DaysInStock = Math.Max(0, (DateTime.UtcNow.Date - allocated.Date).Days)
                    };
                });

            if (scopedSubdealerIds != null)
                rows = rows.Where(r => scopedSubdealerIds.Contains(r.SubdealerId));

            if (request.SubdealerId.HasValue)
                rows = rows.Where(r => r.SubdealerId == request.SubdealerId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                rows = rows.Where(r =>
                    r.ChassisNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.SubdealerName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (r.OrderNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || r.ModelName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.ColorName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return rows
                .OrderBy(r => r.DealershipLocation)
                .ThenBy(r => r.SubdealerName)
                .ThenByDescending(r => r.AllocatedDate)
                .ToList();
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
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
            var vehicles = VehicleLifecycleHelper.FilterActiveLifecycle(await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var bookingsByVehicle = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .GroupBy(b => b.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var orders = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToDictionary(o => o.OrderId);
            var orderItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync())
                .Where(i => i.VehicleId.HasValue)
                .GroupBy(i => i.VehicleId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.ApprovedDate ?? i.CreatedDate).First());
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var allOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();
            var warrantyOnlyVehicleIds = await WarrantyOnlyVehicleFlowHelper.GetWarrantyOnlyVehicleIdsAsync(_unitOfWork);

            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
            HashSet<int>? scopedSubdealerIds = dealershipFilter != null
                ? DealershipQueryScope.GetScopedSubdealerOrgIds(allOrgRoles, dealershipFilter)
                : null;

            if (!string.IsNullOrWhiteSpace(request.DealershipLocation))
            {
                var location = request.DealershipLocation.Trim();
                var locationDealershipIds = dealerships.Values
                    .Where(d => d.IsActive
                        && string.Equals(d.Location?.Trim(), location, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.DealershipId)
                    .ToHashSet();
                var locationSubdealerIds = orgs.Values
                    .Where(o => o.IsActive && locationDealershipIds.Contains(o.DealershipId))
                    .Select(o => o.SubDealerId)
                    .ToHashSet();
                scopedSubdealerIds = scopedSubdealerIds == null
                    ? locationSubdealerIds
                    : scopedSubdealerIds.Intersect(locationSubdealerIds).ToHashSet();
            }

            var rows = vehicles
                .Where(v =>
                {
                    if (warrantyOnlyVehicleIds.Contains(v.VehicleId))
                        return false;

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
                    models.TryGetValue(v.ModelId, out var model);
                    colors.TryGetValue(v.ColorId, out var color);
                    orders.TryGetValue(v.PurchaseOrderId ?? 0, out var order);
                    orderItems.TryGetValue(v.VehicleId, out var item);

                    string? location = null;
                    string? dealershipName = null;
                    if (v.SubdealerId.HasValue
                        && orgs.TryGetValue(v.SubdealerId.Value, out var subDealerOrg)
                        && dealerships.TryGetValue(subDealerOrg.DealershipId, out var dealer))
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
                        SubdealerName = SubdealerOrgService.ResolveOrgDisplayName(v.SubdealerId, orgs),
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
            {
                var orgId = await SubdealerOrgService.ResolveOrgIdAsync(_unitOfWork, request.SubdealerId.Value);
                rows = rows.Where(r => r.SubdealerId == orgId);
            }

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

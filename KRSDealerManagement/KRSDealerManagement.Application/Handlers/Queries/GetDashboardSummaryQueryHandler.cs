using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummary>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetDashboardSummaryQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardSummary> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var summary = new DashboardSummary();
            HashSet<int>? scopedIds = null;

            if (request.SubdealerId.HasValue)
                await LoadSubdealerDashboard(summary, request.SubdealerId.Value);
            else
                scopedIds = await LoadAdminDashboard(summary, request);

            if (request.IncludeRecentActivities)
                await LoadRecentActivities(summary, request.SubdealerId, request.DealershipId, request.DealershipIds, scopedIds);

            return summary;
        }

        private async Task<HashSet<int>?> GetScopedSubdealerIdsAsync(int? dealershipId, IList<int>? dealershipIds)
        {
            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(dealershipId, dealershipIds);
            if (dealershipFilter == null)
                return null;

            var rolesTask = _unitOfWork.Roles.GetAllAsync();
            var orgRolesTask = _unitOfWork.UserOrgRoles.GetAllAsync();
            await Task.WhenAll(rolesTask, orgRolesTask);

            var subRole = (await rolesTask).FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));

            return DealershipQueryScope.GetScopedSubdealerUserIds(await orgRolesTask, dealershipFilter, subRole?.RoleId);
        }

        private static bool IsInScope(int subdealerId, HashSet<int>? scopedIds)
            => scopedIds == null || scopedIds.Contains(subdealerId);

        private async Task<HashSet<int>?> LoadAdminDashboard(DashboardSummary summary, GetDashboardSummaryQuery request)
        {
            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);

            var rolesTask = _unitOfWork.Roles.GetAllAsync();
            var orgRolesTask = _unitOfWork.UserOrgRoles.GetAllAsync();
            var orgsTask = _unitOfWork.SubDealers.GetAllAsync();
            var accountsTask = _unitOfWork.SubdealerAccounts.GetAllAsync();
            var balancesTask = _unitOfWork.AccountBalances.GetAllAsync();
            var ordersTask = _unitOfWork.PurchaseOrders.GetAllAsync();
            var itemsTask = _unitOfWork.PurchaseOrderItems.GetAllAsync();
            var vehiclesTask = _unitOfWork.Vehicles.GetAllAsync();
            var commissionsTask = _unitOfWork.Commissions.GetAllAsync();
            var returnsTask = _unitOfWork.ReturnRequests.GetAllAsync();
            var bookingsTask = _unitOfWork.VehicleBookings.GetAllAsync();
            var dealerStockTask = _unitOfWork.VehicleMasters.GetAllAsync();
            var paymentsTask = request.IncludePaymentPending
                ? _unitOfWork.Payments.GetAllAsync()
                : Task.FromResult<IEnumerable<Payment>>(Array.Empty<Payment>());

            await Task.WhenAll(
                rolesTask,
                orgRolesTask,
                orgsTask,
                accountsTask,
                balancesTask,
                ordersTask,
                itemsTask,
                vehiclesTask,
                commissionsTask,
                returnsTask,
                bookingsTask,
                dealerStockTask,
                paymentsTask);

            var orgRoles = (await orgRolesTask).ToList();
            var subRole = (await rolesTask).FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
            var scopedIds = DealershipQueryScope.GetScopedSubdealerUserIds(orgRoles, dealershipFilter, subRole?.RoleId);

            var orgs = await orgsTask;
            summary.TotalSubdealers = orgs.Count(o =>
                o.IsActive && DealershipQueryScope.MatchesDealership(o.DealershipId, dealershipFilter));

            var accounts = (await accountsTask).ToList();
            summary.TotalAccounts = accounts.Count(a => a.IsActive && IsInScope(a.SubdealerId, scopedIds));

            var balances = await balancesTask;
            var scopedBalances = balances.Where(b => IsInScope(b.SubdealerId, scopedIds));
            summary.TotalBalance = scopedBalances.Sum(b => b.CurrentBalance);
            summary.TotalReservedAmount = scopedBalances.Sum(b => b.ReservedAmount);

            var orders = (await ordersTask).ToList();
            var allItems = (await itemsTask).ToList();
            var allVehicles = (await vehiclesTask).ToList();
            var vehiclesByOrderId = allVehicles
                .Where(v => v.PurchaseOrderId.HasValue)
                .GroupBy(v => v.PurchaseOrderId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Vehicle>)g.ToList());
            var itemsByOrderId = allItems
                .GroupBy(i => i.PurchaseOrderId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<PurchaseOrderItem>)g.ToList());

            summary.PendingPurchaseOrders = orders.Count(o =>
            {
                if (!IsInScope(o.SubdealerId, scopedIds)) return false;
                vehiclesByOrderId.TryGetValue(o.OrderId, out var orderVehicles);
                itemsByOrderId.TryGetValue(o.OrderId, out var orderItems);
                return VehicleStatusResolver.ResolveOrderDisplayStatus(
                        orderVehicles ?? Array.Empty<Vehicle>(),
                        orderItems ?? Array.Empty<PurchaseOrderItem>())
                    == UnifiedVehicleStatus.Submitted;
            });

            var commissions = await commissionsTask;
            summary.PendingCommissions = commissions.Count(c =>
                c.CanBeApproved() && IsInScope(c.SubdealerId, scopedIds));

            var returns = (await returnsTask).ToList();
            var accountSubdealerById = accounts.ToDictionary(a => a.AccountId, a => a.SubdealerId);
            var vehicleSubdealerById = allVehicles.ToDictionary(v => v.VehicleId, v => v.SubdealerId);
            var orderSubdealerById = orders.ToDictionary(o => o.OrderId, o => o.SubdealerId);
            summary.PendingReturnRequests = ReturnRequestScopeHelper.CountPending(
                returns,
                scopedIds,
                accountSubdealerById,
                vehicleSubdealerById,
                orderSubdealerById);

            if (request.IncludePaymentPending)
            {
                var payments = await paymentsTask;
                summary.PendingPayments = payments.Count(p =>
                    p.Status == 0 && IsInScope(p.SubdealerId, scopedIds));
            }
            else
            {
                summary.PendingPayments = 0;
            }

            var allBookings = (await bookingsTask).ToList();
            LoadBookingStatusCounts(summary, allVehicles, scopedIds, allBookings);
            summary.ShowroomStockCount = CountShowroomStock(allVehicles, scopedIds, allBookings);

            var dealerStock = await dealerStockTask;
            summary.DealerStockCount = dealerStock.Count(m =>
                !m.IsAllocated && DealershipQueryScope.MatchesDealership(m.DealershipId, dealershipFilter));

            return scopedIds;
        }

        private static int CountShowroomStock(
            IEnumerable<Vehicle> vehicles,
            HashSet<int>? scopedIds,
            IEnumerable<VehicleBooking> bookings)
        {
            var bookingsByVehicle = bookings
                .GroupBy(b => b.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());

            return vehicles.Count(v =>
            {
                if (!v.SubdealerId.HasValue || !IsInScope(v.SubdealerId.Value, scopedIds))
                    return false;

                bookingsByVehicle.TryGetValue(v.VehicleId, out var booking);
                return ShowroomStockFilter.IsShowroomStock(
                    v.Status,
                    v.SubdealerId,
                    booking?.InvoiceDate,
                    booking != null);
            });
        }

        private static void LoadBookingStatusCounts(
            DashboardSummary summary,
            IEnumerable<Vehicle> vehicles,
            HashSet<int>? scopedIds,
            IEnumerable<VehicleBooking> bookings)
        {
            var vehicleById = vehicles.ToDictionary(v => v.VehicleId);

            int Count(int stageStatus) => bookings.Count(b =>
            {
                if (!IsInScope(b.SubdealerId, scopedIds))
                    return false;

                if (!vehicleById.TryGetValue(b.VehicleId, out var vehicle))
                    return false;

                return BookingStageFilter.MatchesStage(
                    vehicle.Status,
                    stageStatus,
                    b.PaperReceivedDate,
                    b.InvoiceDate,
                    b.InsuranceDate,
                    b.AgentDate,
                    b.RegistrationDate,
                    b.SubsidyId);
            });

            summary.BookedToCustomerCount = Count(UnifiedVehicleStatus.BookedToCustomer);
            summary.PaperReceivedCount = Count(UnifiedVehicleStatus.PaperReceived);
            summary.InvoicedCount = Count(UnifiedVehicleStatus.Invoiced);
            summary.InsuranceCreatedCount = Count(UnifiedVehicleStatus.InsuranceCreated);
            summary.RtoRequestedCount = Count(UnifiedVehicleStatus.RtoRequested);
            summary.RegisteredCount = bookings.Count(b =>
            {
                if (!IsInScope(b.SubdealerId, scopedIds))
                    return false;
                if (!vehicleById.TryGetValue(b.VehicleId, out var vehicle))
                    return false;
                return BookingStageFilter.IsRegisteredAwaitingNumberPlate(
                    vehicle.Status,
                    b.PaperReceivedDate,
                    b.InvoiceDate,
                    b.InsuranceDate,
                    b.AgentDate,
                    b.RegistrationDate,
                    b.SubsidyId,
                    b.NumberPlateReceivedDate,
                    b.NumberPlateReceivedBy);
            });

            summary.SubsidyIdPendingCount = bookings.Count(b =>
            {
                if (!IsInScope(b.SubdealerId, scopedIds))
                    return false;
                if (!vehicleById.TryGetValue(b.VehicleId, out var vehicle))
                    return false;
                return BookingStageFilter.IsSubsidyIdPending(
                    b.InvoiceDate,
                    b.InsuranceDate,
                    b.SubsidyId,
                    vehicle.Status);
            });

            summary.SubsidyDocsPendingCount = bookings.Count(b =>
            {
                if (!IsInScope(b.SubdealerId, scopedIds))
                    return false;
                if (!vehicleById.TryGetValue(b.VehicleId, out var vehicle))
                    return false;
                return BookingStageFilter.IsSubsidyDocsPending(
                    b.SubsidyId,
                    b.FaceVerificationPath,
                    b.RcImagePath,
                    b.BoothPhotoPath,
                    b.SubsidyUndertakingPath,
                    vehicle.Status);
            });
        }

        private async Task LoadSubdealerDashboard(DashboardSummary summary, int subdealerId)
        {
            var accountsTask = _unitOfWork.SubdealerAccounts.GetAllAsync();
            var balancesTask = _unitOfWork.AccountBalances.GetAllAsync();
            var ordersTask = _unitOfWork.PurchaseOrders.GetAllAsync();
            var itemsTask = _unitOfWork.PurchaseOrderItems.GetAllAsync();
            var vehiclesTask = _unitOfWork.Vehicles.GetAllAsync();
            var commissionsTask = _unitOfWork.Commissions.GetAllAsync();
            var paymentsTask = _unitOfWork.Payments.GetAllAsync();
            var bookingsTask = _unitOfWork.VehicleBookings.GetAllAsync();
            var orgUserIdsTask = SubdealerOrgService.GetOrgLoginUserIdsAsync(_unitOfWork, subdealerId);
            var returnsTask = _unitOfWork.ReturnRequests.GetAllAsync();

            await Task.WhenAll(
                accountsTask,
                balancesTask,
                ordersTask,
                itemsTask,
                vehiclesTask,
                commissionsTask,
                paymentsTask,
                bookingsTask,
                orgUserIdsTask,
                returnsTask);

            var accounts = (await accountsTask).ToList();
            var subdealerAccounts = accounts
                .Where(a => a.SubdealerId == subdealerId && a.IsActive)
                .ToList();
            summary.TotalAccounts = subdealerAccounts.Count;

            var balances = await balancesTask;
            var myBalances = balances.Where(b => b.SubdealerId == subdealerId).ToList();
            summary.TotalBalance = myBalances.Sum(b => b.CurrentBalance);
            summary.TotalReservedAmount = myBalances.Sum(b => b.ReservedAmount);

            var orders = (await ordersTask).ToList();
            var allItems = (await itemsTask).ToList();
            var allVehicles = (await vehiclesTask).ToList();
            var vehiclesByOrderId = allVehicles
                .Where(v => v.PurchaseOrderId.HasValue)
                .GroupBy(v => v.PurchaseOrderId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Vehicle>)g.ToList());
            var itemsByOrderId = allItems
                .GroupBy(i => i.PurchaseOrderId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<PurchaseOrderItem>)g.ToList());

            summary.PendingPurchaseOrders = orders.Count(o =>
            {
                if (o.SubdealerId != subdealerId) return false;
                vehiclesByOrderId.TryGetValue(o.OrderId, out var orderVehicles);
                itemsByOrderId.TryGetValue(o.OrderId, out var orderItems);
                return VehicleStatusResolver.ResolveOrderDisplayStatus(
                        orderVehicles ?? Array.Empty<Vehicle>(),
                        orderItems ?? Array.Empty<PurchaseOrderItem>())
                    == UnifiedVehicleStatus.Submitted;
            });

            var commissions = await commissionsTask;
            summary.PendingCommissions = commissions.Count(c => c.SubdealerId == subdealerId && c.CanBeApproved());

            var orgUserIds = await orgUserIdsTask;
            var returns = await returnsTask;
            var accountSubdealerById = accounts.ToDictionary(a => a.AccountId, a => a.SubdealerId);
            var vehicleSubdealerById = allVehicles.ToDictionary(v => v.VehicleId, v => v.SubdealerId);
            var orderSubdealerById = orders.ToDictionary(o => o.OrderId, o => o.SubdealerId);
            summary.PendingReturnRequests = ReturnRequestScopeHelper.CountPending(
                returns,
                orgUserIds,
                accountSubdealerById,
                vehicleSubdealerById,
                orderSubdealerById);

            var payments = await paymentsTask;
            summary.PendingPayments = payments.Count(p => p.SubdealerId == subdealerId && p.Status == 0);

            var scopedIds = new HashSet<int> { subdealerId };
            LoadBookingStatusCounts(summary, allVehicles, scopedIds, await bookingsTask);
        }

        private async Task LoadRecentActivities(
            DashboardSummary summary,
            int? subdealerId,
            int? dealershipId,
            IList<int>? dealershipIds,
            HashSet<int>? scopedIds)
        {
            HashSet<int>? userFilter = null;

            if (subdealerId.HasValue)
            {
                userFilter = new HashSet<int> { subdealerId.Value };
            }
            else if (scopedIds != null)
            {
                userFilter = scopedIds;
            }
            else if (DealershipQueryScope.ResolveDealershipIds(dealershipId, dealershipIds) != null)
            {
                userFilter = await GetScopedSubdealerIdsAsync(dealershipId, dealershipIds);
            }

            var sinceUtc = DateTime.UtcNow.AddDays(-30);
            var auditLogs = await _unitOfWork.AuditLogs.GetRecentAsync(sinceUtc, 50, userFilter?.ToList());

            summary.RecentActivities = auditLogs
                .Select(a => new RecentActivityItem
                {
                    ActivityId = a.AuditLogId,
                    ActivityType = a.Action,
                    Description = $"{a.EntityType} — {a.Action}",
                    CreatedDate = a.CreatedDate,
                    UserName = a.UserRole
                })
                .ToList();
        }
    }
}

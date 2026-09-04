using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
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

            if (request.SubdealerId.HasValue)
                await LoadSubdealerDashboard(summary, request.SubdealerId.Value);
            else
                await LoadAdminDashboard(summary, request);

            if (request.IncludeRecentActivities)
                await LoadRecentActivities(summary, request.SubdealerId, request.DealershipId);

            return summary;
        }

        private async Task<HashSet<int>?> GetScopedSubdealerIdsAsync(int? dealershipId)
        {
            if (!dealershipId.HasValue)
                return null;

            var roles = await _unitOfWork.Roles.GetAllAsync();
            var subRole = roles.FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));
            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => subRole == null || a.RoleId == subRole.RoleId)
                .Where(a => a.DealershipId == dealershipId.Value)
                .Select(a => a.UserId)
                .ToHashSet();

            return assignments;
        }

        private static bool IsInScope(int subdealerId, HashSet<int>? scopedIds)
            => scopedIds == null || scopedIds.Contains(subdealerId);

        private async Task LoadAdminDashboard(DashboardSummary summary, GetDashboardSummaryQuery request)
        {
            var scopedIds = await GetScopedSubdealerIdsAsync(request.DealershipId);

            var users = await _unitOfWork.Users.GetAllAsync();
            var orgs = await _unitOfWork.SubDealers.GetAllAsync();
            summary.TotalSubdealers = orgs.Count(o =>
                o.IsActive && (!request.DealershipId.HasValue || o.DealershipId == request.DealershipId));

            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            summary.TotalAccounts = accounts.Count(a => a.IsActive && IsInScope(a.SubdealerId, scopedIds));

            var balances = await _unitOfWork.AccountBalances.GetAllAsync();
            var scopedBalances = balances.Where(b => IsInScope(b.SubdealerId, scopedIds));
            summary.TotalBalance = scopedBalances.Sum(b => b.CurrentBalance);
            summary.TotalReservedAmount = scopedBalances.Sum(b => b.ReservedAmount);

            var orders = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToList();
            var allItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync()).ToList();
            var allVehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            summary.PendingPurchaseOrders = orders.Count(o =>
            {
                if (!IsInScope(o.SubdealerId, scopedIds)) return false;
                var orderVehicles = allVehicles.Where(v => v.PurchaseOrderId == o.OrderId).ToList();
                var orderItems = allItems.Where(i => i.PurchaseOrderId == o.OrderId).ToList();
                return VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, orderItems)
                    == UnifiedVehicleStatus.Submitted;
            });

            var commissions = await _unitOfWork.Commissions.GetAllAsync();
            summary.PendingCommissions = commissions.Count(c =>
                c.CanBeApproved() && IsInScope(c.SubdealerId, scopedIds));

            summary.PendingReturnRequests = await CountPendingReturnRequestsAsync(scopedIds);

            if (request.IncludePaymentPending)
            {
                var payments = await _unitOfWork.Payments.GetAllAsync();
                summary.PendingPayments = payments.Count(p =>
                    p.Status == 0 && IsInScope(p.SubdealerId, scopedIds));
            }
            else
            {
                summary.PendingPayments = 0;
            }

            var allBookings = await _unitOfWork.VehicleBookings.GetAllAsync();
            LoadBookingStatusCounts(summary, allVehicles, scopedIds, allBookings);
            summary.ShowroomStockCount = CountShowroomStock(allVehicles, scopedIds, allBookings);

            var dealerStock = await _unitOfWork.VehicleMasters.GetAllAsync();
            summary.DealerStockCount = dealerStock.Count(m =>
                !m.IsAllocated
                && (!request.DealershipId.HasValue || m.DealershipId == request.DealershipId));
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
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var subdealerAccounts = accounts
                .Where(a => a.SubdealerId == subdealerId && a.IsActive)
                .ToList();
            summary.TotalAccounts = subdealerAccounts.Count;

            var balances = await _unitOfWork.AccountBalances.GetAllAsync();
            var myBalances = balances.Where(b => b.SubdealerId == subdealerId).ToList();
            summary.TotalBalance = myBalances.Sum(b => b.CurrentBalance);
            summary.TotalReservedAmount = myBalances.Sum(b => b.ReservedAmount);

            var orders = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToList();
            var allItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync()).ToList();
            var allVehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            summary.PendingPurchaseOrders = orders.Count(o =>
            {
                if (o.SubdealerId != subdealerId) return false;
                var orderVehicles = allVehicles.Where(v => v.PurchaseOrderId == o.OrderId).ToList();
                var orderItems = allItems.Where(i => i.PurchaseOrderId == o.OrderId).ToList();
                return VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, orderItems)
                    == UnifiedVehicleStatus.Submitted;
            });

            var commissions = await _unitOfWork.Commissions.GetAllAsync();
            summary.PendingCommissions = commissions.Count(c => c.SubdealerId == subdealerId && c.CanBeApproved());

            var orgUserIds = await SubdealerOrgService.GetOrgLoginUserIdsAsync(_unitOfWork, subdealerId);
            summary.PendingReturnRequests = await CountPendingReturnRequestsAsync(orgUserIds);

            var payments = await _unitOfWork.Payments.GetAllAsync();
            summary.PendingPayments = payments.Count(p => p.SubdealerId == subdealerId && p.Status == 0);

            var scopedIds = new HashSet<int> { subdealerId };
            LoadBookingStatusCounts(summary, allVehicles, scopedIds, await _unitOfWork.VehicleBookings.GetAllAsync());
        }

        private async Task LoadRecentActivities(DashboardSummary summary, int? subdealerId, int? dealershipId)
        {
            var auditLogs = await _unitOfWork.AuditLogs.GetAllAsync();
            HashSet<int>? scopedIds = null;

            if (subdealerId.HasValue)
            {
                scopedIds = new HashSet<int> { subdealerId.Value };
            }
            else if (dealershipId.HasValue)
            {
                scopedIds = await GetScopedSubdealerIdsAsync(dealershipId);
            }

            var sinceUtc = DateTime.UtcNow.AddDays(-30);

            var filtered = scopedIds == null
                ? auditLogs
                : auditLogs.Where(a => scopedIds.Contains(a.UserId));

            filtered = filtered.Where(a => a.CreatedDate >= sinceUtc);

            summary.RecentActivities = filtered
                .OrderByDescending(a => a.CreatedDate)
                .Take(50)
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

        private async Task<int> CountPendingReturnRequestsAsync(HashSet<int>? scopedOrgUserIds)
        {
            var returns = await _unitOfWork.ReturnRequests.GetAllAsync();
            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            var vehicles = await _unitOfWork.Vehicles.GetAllAsync();
            var orders = await _unitOfWork.PurchaseOrders.GetAllAsync();

            var accountSubdealerById = accounts.ToDictionary(a => a.AccountId, a => a.SubdealerId);
            var vehicleSubdealerById = vehicles.ToDictionary(v => v.VehicleId, v => v.SubdealerId);
            var orderSubdealerById = orders.ToDictionary(o => o.OrderId, o => o.SubdealerId);

            return ReturnRequestScopeHelper.CountPending(
                returns,
                scopedOrgUserIds,
                accountSubdealerById,
                vehicleSubdealerById,
                orderSubdealerById);
        }
    }
}

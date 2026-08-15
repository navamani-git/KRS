using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

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
                await LoadAdminDashboard(summary);

            if (request.IncludeRecentActivities)
                await LoadRecentActivities(summary, request.SubdealerId);

            return summary;
        }

        private async Task LoadAdminDashboard(DashboardSummary summary)
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            summary.TotalSubdealers = users.Count(u => u.UserRole == 2 && u.IsActive);

            var accounts = await _unitOfWork.SubdealerAccounts.GetAllAsync();
            summary.TotalAccounts = accounts.Count(a => a.IsActive);

            var balances = await _unitOfWork.AccountBalances.GetAllAsync();
            summary.TotalBalance = balances.Sum(b => b.CurrentBalance);
            summary.TotalReservedAmount = balances.Sum(b => b.ReservedAmount);

            var orders = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToList();
            var allItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync()).ToList();
            var allVehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            summary.PendingPurchaseOrders = orders.Count(o =>
            {
                var orderVehicles = allVehicles.Where(v => v.PurchaseOrderId == o.OrderId).ToList();
                var orderItems = allItems.Where(i => i.PurchaseOrderId == o.OrderId).ToList();
                return VehicleStatusResolver.ResolveOrderDisplayStatus(orderVehicles, orderItems)
                    == UnifiedVehicleStatus.Submitted;
            });

            var commissions = await _unitOfWork.Commissions.GetAllAsync();
            summary.PendingCommissions = commissions.Count(c => c.CanBeApproved());

            var vehicles = await _unitOfWork.Vehicles.GetAllAsync();
            summary.PendingReturnRequests = vehicles.Count(v => v.Status == UnifiedVehicleStatus.ReturnRequested);

            var payments = await _unitOfWork.Payments.GetAllAsync();
            summary.PendingPayments = payments.Count(p => p.Status == 0);
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

            summary.PendingReturnRequests = allVehicles.Count(v =>
                v.SubdealerId == subdealerId && v.Status == UnifiedVehicleStatus.ReturnRequested);

            var payments = await _unitOfWork.Payments.GetAllAsync();
            summary.PendingPayments = payments.Count(p => p.SubdealerId == subdealerId && p.Status == 0);
        }

        private async Task LoadRecentActivities(DashboardSummary summary, int? subdealerId)
        {
            var auditLogs = await _unitOfWork.AuditLogs.GetAllAsync();

            var filtered = subdealerId.HasValue
                ? auditLogs.Where(a => a.UserId == subdealerId.Value)
                : auditLogs;

            summary.RecentActivities = filtered
                .OrderByDescending(a => a.CreatedDate)
                .Take(10)
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

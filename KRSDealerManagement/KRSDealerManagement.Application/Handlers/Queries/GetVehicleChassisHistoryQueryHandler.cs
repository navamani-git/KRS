using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehicleChassisHistoryQueryHandler : IRequestHandler<GetVehicleChassisHistoryQuery, VehicleChassisHistoryDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;
        private static readonly TimeZoneInfo IndiaTimeZone = ResolveIndiaTimeZone();

        private sealed class RawEvent
        {
            public DateTime OccurredAt { get; init; }
            public int StatusValue { get; init; }
            public required string Description { get; init; }
            public string? Actor { get; init; }
            public string? Location { get; init; }
            public string? OrderNumber { get; init; }
        }

        private static TimeZoneInfo ResolveIndiaTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }

        private static DateTime ToIndiaTime(DateTime utcOrUnspecified)
        {
            var utc = utcOrUnspecified.Kind switch
            {
                DateTimeKind.Utc => utcOrUnspecified,
                DateTimeKind.Local => utcOrUnspecified.ToUniversalTime(),
                _ => DateTime.SpecifyKind(utcOrUnspecified, DateTimeKind.Utc)
            };
            return TimeZoneInfo.ConvertTimeFromUtc(utc, IndiaTimeZone);
        }

        private static DateTime ToMinuteKey(DateTime local) =>
            new(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0);

        public GetVehicleChassisHistoryQueryHandler(IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<VehicleChassisHistoryDto?> Handle(GetVehicleChassisHistoryQuery request, CancellationToken cancellationToken)
        {
            var chassis = request.ChassisNumber?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(chassis) || UnifiedVehicleStatus.IsPlaceholderChassis(chassis))
                return null;

            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var vehicle = vehicles
                .Where(v => !UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber))
                .FirstOrDefault(v => string.Equals(v.ChassisNumber?.Trim(), chassis, StringComparison.OrdinalIgnoreCase));

            if (vehicle == null)
                return null;

            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var userOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();
            var ordersById = (await _unitOfWork.PurchaseOrders.GetAllAsync()).ToDictionary(o => o.OrderId);
            var accounts = (await _unitOfWork.SubdealerAccounts.GetAllAsync()).ToDictionary(a => a.AccountId);
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Vehicle);
            var raw = new List<RawEvent>();

            string ResolveSubdealerName(int? userId)
            {
                if (!userId.HasValue || userId.Value <= 0) return "Dealer Showroom";
                var assignment = userOrgRoles
                    .Where(a => a.UserId == userId.Value && a.IsActive)
                    .OrderByDescending(a => a.IsPrimary)
                    .FirstOrDefault();
                if (assignment?.SubDealerId is int orgId && orgs.TryGetValue(orgId, out var org))
                {
                    var location = string.IsNullOrWhiteSpace(org.Location) ? "" : $" ({org.Location})";
                    return $"{org.SubDealerName}{location}";
                }

                return users.TryGetValue(userId.Value, out var user)
                    ? user.GetFullName()
                    : $"Subdealer #{userId}";
            }

            string ResolveDealershipName(int? userId)
            {
                if (!userId.HasValue) return "Dealer Showroom";
                var assignment = userOrgRoles.FirstOrDefault(a => a.UserId == userId.Value && a.IsActive);
                if (assignment?.DealershipId is int dealerId && dealerships.TryGetValue(dealerId, out var dealer))
                    return dealer.DealershipName;
                return "Dealership";
            }

            string StatusName(int statusValue) =>
                statusMap.TryGetValue(statusValue, out var st) ? st.StatusName : $"Status #{statusValue}";

            int StatusSort(int statusValue) =>
                statusMap.TryGetValue(statusValue, out var st) ? st.SortOrder : statusValue;

            void Add(DateTime at, int status, string description, string? actor = null, string? location = null, string? order = null)
            {
                raw.Add(new RawEvent
                {
                    OccurredAt = at,
                    StatusValue = status,
                    Description = description,
                    Actor = actor,
                    Location = location,
                    OrderNumber = order
                });
            }

            string? primaryOrderNumber = null;

            if (vehicle.PurchaseOrderId.HasValue
                && ordersById.TryGetValue(vehicle.PurchaseOrderId.Value, out var order))
            {
                primaryOrderNumber = order.OrderNumber;
                var subdealer = ResolveSubdealerName(order.SubdealerId);

                Add(
                    order.CreatedDate,
                    UnifiedVehicleStatus.Submitted,
                    $"Order {order.OrderNumber} — {subdealer}.",
                    subdealer,
                    ResolveDealershipName(order.SubdealerId),
                    order.OrderNumber);

                var item = (await _unitOfWork.PurchaseOrderItems.GetAllAsync())
                    .Where(i => i.PurchaseOrderId == order.OrderId && i.VehicleId == vehicle.VehicleId)
                    .OrderByDescending(i => i.ApprovedDate ?? i.CreatedDate)
                    .FirstOrDefault();

                if (item != null && item.Status == 1)
                {
                    Add(
                        item.ApprovedDate ?? order.ApprovedDate ?? order.CreatedDate,
                        UnifiedVehicleStatus.ApprovedByDealer,
                        $"Chassis {chassis} allocated to {subdealer} (order {order.OrderNumber}).",
                        "Dealer",
                        ResolveDealershipName(order.SubdealerId),
                        order.OrderNumber);
                }
            }

            var returns = (await _unitOfWork.ReturnRequests.GetAllAsync())
                .Where(r => r.VehicleId == vehicle.VehicleId)
                .OrderBy(r => r.CreatedDate);

            foreach (var ret in returns)
            {
                accounts.TryGetValue(ret.AccountId, out var account);
                var accountLabel = account?.AccountName ?? $"Account #{ret.AccountId}";
                ordersById.TryGetValue(ret.OrderId, out var retOrder);
                var orderNumber = retOrder?.OrderNumber ?? primaryOrderNumber;
                var holderName = account != null
                    ? ResolveSubdealerName(account.SubdealerId)
                    : ResolveSubdealerName(retOrder?.SubdealerId);

                Add(
                    ret.CreatedDate,
                    UnifiedVehicleStatus.ReturnRequested,
                    $"{holderName} — {ret.ReturnReason}",
                    accountLabel,
                    holderName,
                    orderNumber);

                if (!ret.ProcessedDate.HasValue) continue;

                if (ret.Status == 1)
                {
                    Add(
                        ret.ProcessedDate.Value,
                        UnifiedVehicleStatus.ReturnApproved,
                        $"Refund ₹{ret.RefundAmount:N2} to {accountLabel}. Moved to dealer showroom.",
                        ret.ProcessedBy.HasValue && users.TryGetValue(ret.ProcessedBy.Value, out var admin)
                            ? admin.GetFullName()
                            : "Dealer",
                        "Dealer Showroom",
                        orderNumber);
                }
                else if (ret.Status == 2)
                {
                    Add(
                        ret.ProcessedDate.Value,
                        UnifiedVehicleStatus.ReturnCancelled,
                        ret.AdminRemarks ?? "Return rejected.",
                        ret.ProcessedBy.HasValue && users.TryGetValue(ret.ProcessedBy.Value, out var admin)
                            ? admin.GetFullName()
                            : "Dealer",
                        holderName,
                        orderNumber);
                }
            }

            var auditLogs = (await _unitOfWork.AuditLogs.GetAllAsync())
                .Where(a => a.EntityType == "Vehicle" && a.EntityId == vehicle.VehicleId)
                .OrderBy(a => a.CreatedDate);

            foreach (var log in auditLogs.Where(a =>
                         a.Action.Equals("AllocateToSubdealer", StringComparison.OrdinalIgnoreCase)))
            {
                var payload = TryParseJson(log.NewValue);
                var root = payload?.RootElement;
                var subdealerName = root?.TryGetProperty("SubdealerName", out var sn) == true
                    ? sn.GetString()
                    : null;
                var amount = root?.TryGetProperty("Amount", out var amt) == true
                    ? amt.GetDecimal()
                    : vehicle.CurrentPrice;

                Add(
                    log.CreatedDate,
                    UnifiedVehicleStatus.ApprovedByDealer,
                    $"Allocated to {subdealerName ?? "subdealer"} from showroom. Debited ₹{amount:N2}.",
                    log.UserId > 0 && users.TryGetValue(log.UserId, out var actor)
                        ? actor.GetFullName()
                        : "Dealer",
                    subdealerName ?? ResolveSubdealerName(vehicle.SubdealerId),
                    primaryOrderNumber);
            }

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);

            if (booking != null)
            {
                var bookingSubdealer = ResolveSubdealerName(booking.SubdealerId);
                var customer = booking.CustomerName;

                Add(
                    booking.SubmittedDate,
                    UnifiedVehicleStatus.BookedToCustomer,
                    $"Customer {customer} ({booking.CustomerMobile}) at {bookingSubdealer}.",
                    bookingSubdealer,
                    ResolveDealershipName(booking.SubdealerId),
                    primaryOrderNumber);

                AddBookingMilestone(booking.PaperReceivedDate, UnifiedVehicleStatus.PaperReceived, bookingSubdealer, primaryOrderNumber);
                AddBookingMilestone(booking.InvoiceDate, UnifiedVehicleStatus.Invoiced, bookingSubdealer, primaryOrderNumber);
                AddBookingMilestone(booking.InsuranceDate, UnifiedVehicleStatus.InsuranceCreated, bookingSubdealer, primaryOrderNumber);
                AddBookingMilestone(booking.AgentDate, UnifiedVehicleStatus.RtoRequested, bookingSubdealer, primaryOrderNumber);
                AddBookingMilestone(booking.RegistrationDate, UnifiedVehicleStatus.Registered, bookingSubdealer, primaryOrderNumber,
                    string.IsNullOrWhiteSpace(booking.RtoNumber) ? null : $"RTO {booking.RtoNumber}");

                var latestMilestoneDate = LatestDate(
                    booking.PaperReceivedDate,
                    booking.InvoiceDate,
                    booking.InsuranceDate,
                    booking.AgentDate,
                    booking.RegistrationDate,
                    booking.NumberPlateReceivedDate);

                if (vehicle.Status >= UnifiedVehicleStatus.SubsidyIdCreated
                    && !string.IsNullOrWhiteSpace(booking.SubsidyId))
                {
                    // Subsidy is assigned after prior milestones — never date it before them
                    var subsidyAt = LatestDate(
                        booking.SubsidyDocsSubmittedDate,
                        latestMilestoneDate,
                        booking.ModifiedDate) ?? booking.ModifiedDate;

                    Add(
                        subsidyAt,
                        UnifiedVehicleStatus.SubsidyIdCreated,
                        $"Subsidy ID {booking.SubsidyId.Trim()}",
                        bookingSubdealer,
                        bookingSubdealer,
                        primaryOrderNumber);
                }

                if (vehicle.Status >= UnifiedVehicleStatus.Delivered)
                {
                    var deliveredAt = LatestDate(
                        booking.NumberPlateReceivedDate,
                        latestMilestoneDate,
                        booking.ModifiedDate) ?? booking.ModifiedDate;

                    Add(
                        deliveredAt,
                        UnifiedVehicleStatus.Delivered,
                        $"Delivered to {customer}.",
                        bookingSubdealer,
                        ResolveDealershipName(booking.SubdealerId),
                        primaryOrderNumber);
                }
            }

            static DateTime? LatestDate(params DateTime?[] dates)
            {
                var set = dates.Where(d => d.HasValue).Select(d => d!.Value).ToList();
                return set.Count == 0 ? null : set.Max();
            }

            void AddBookingMilestone(DateTime? date, int status, string subdealer, string? order, string? detail = null)
            {
                if (!date.HasValue) return;
                Add(date.Value, status, detail ?? string.Empty, subdealer, subdealer, order);
            }

            var combined = raw
                .Select(r => new
                {
                    Raw = r,
                    Local = ToIndiaTime(r.OccurredAt),
                    MinuteKey = ToMinuteKey(ToIndiaTime(r.OccurredAt))
                })
                .OrderBy(x => x.Raw.OccurredAt)
                .GroupBy(x => x.MinuteKey)
                .OrderBy(g => g.Key)
                .Select((g, index) =>
                {
                    var items = g.OrderBy(x => x.Raw.OccurredAt).ThenBy(x => StatusSort(x.Raw.StatusValue)).ToList();
                    var first = items[0].Raw;
                    var distinctStatuses = items.Select(x => x.Raw.StatusValue).Distinct().OrderBy(StatusSort).ToList();

                    string Title() => string.Join(" / ", distinctStatuses.Select(StatusName));
                    string Description() => MergeDescriptions(items.Select(x => x.Raw).ToList());

                    string? Actor() => items.Select(x => x.Raw.Actor).FirstOrDefault(a => !string.IsNullOrWhiteSpace(a));
                    string? Location() => items.Select(x => x.Raw.Location).LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
                    string? Order() => items.Select(x => x.Raw.OrderNumber).FirstOrDefault(o => !string.IsNullOrWhiteSpace(o));

                    var primaryStatus = distinctStatuses[0];
                    statusMap.TryGetValue(primaryStatus, out var st);

                    return new VehicleChassisHistoryEventDto
                    {
                        Step = index + 1,
                        OccurredAt = items.Min(x => x.Raw.OccurredAt),
                        OccurredAtLocal = g.Key,
                        StatusValue = primaryStatus,
                        StatusBadgeClass = st?.BadgeClass,
                        Title = Title(),
                        Description = Description(),
                        Actor = Actor(),
                        Location = Location(),
                        OrderNumber = Order()
                    };
                })
                .ToList();

            statusMap.TryGetValue(vehicle.Status, out var currentStatus);

            return new VehicleChassisHistoryDto
            {
                VehicleId = vehicle.VehicleId,
                ChassisNumber = chassis,
                ModelName = models.TryGetValue(vehicle.ModelId, out var model) ? model.ModelName : $"Model #{vehicle.ModelId}",
                ColorName = colors.TryGetValue(vehicle.ColorId, out var color) ? color.ColorName : $"Color #{vehicle.ColorId}",
                CurrentStatus = vehicle.Status,
                CurrentStatusName = currentStatus?.StatusName,
                CurrentHolder = vehicle.SubdealerId.HasValue
                    ? ResolveSubdealerName(vehicle.SubdealerId)
                    : "Dealer Showroom",
                Events = combined
            };
        }

        private static string MergeDescriptions(IReadOnlyList<RawEvent> items)
        {
            var parts = items
                .Select(x => x.Description?.Trim())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return parts.Count == 0 ? "—" : string.Join(" · ", parts);
        }

        private static JsonDocument? TryParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonDocument.Parse(json); }
            catch { return null; }
        }
    }
}

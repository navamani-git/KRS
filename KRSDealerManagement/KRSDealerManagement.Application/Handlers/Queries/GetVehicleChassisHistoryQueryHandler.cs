using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
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

            var master = vehicle?.VehicleMasterId > 0
                ? await _unitOfWork.VehicleMasters.GetByIdAsync(vehicle.VehicleMasterId)
                : await _unitOfWork.VehicleMasters.GetByChassisAsync(chassis);

            if (vehicle == null && master == null)
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
            var subdealerHistoryActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

            if (master != null)
            {
                foreach (var h in await _unitOfWork.VehicleMasters.GetHistoryAsync(master.VehicleMasterId))
                {
                    var actor = h.UserId.HasValue && users.TryGetValue(h.UserId.Value, out var u)
                        ? u.GetFullName()
                        : "Staff";
                    var status = VehicleHistoryHelper.ActionToStatus(h.Action) ?? UnifiedVehicleStatus.Submitted;
                    Add(h.CreatedDate, status,
                        string.IsNullOrWhiteSpace(h.Remarks) ? h.Action : $"{h.Action} — {h.Remarks}",
                        actor, "Dealer Stock", null);
                }
            }

            if (vehicle != null)
            {
                foreach (var h in await _unitOfWork.SubdealerVehicleHistories.GetBySubdealerVehicleIdAsync(vehicle.VehicleId))
                {
                    subdealerHistoryActions.Add(h.Action);
                    var actor = h.UserId.HasValue && users.TryGetValue(h.UserId.Value, out var u)
                        ? u.GetFullName()
                        : "Staff";
                    var status = VehicleHistoryHelper.ActionToStatus(h.Action) ?? vehicle.Status;
                    Add(h.CreatedDate, status,
                        string.IsNullOrWhiteSpace(h.Remarks) ? h.Action : $"{h.Action} — {h.Remarks}",
                        actor, ResolveSubdealerName(vehicle.SubdealerId), null);
                }
            }

            string? primaryOrderNumber = null;

            if (vehicle?.PurchaseOrderId is int poId && ordersById.TryGetValue(poId, out var order))
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

                if (item != null && item.Status == 1
                    && !subdealerHistoryActions.Contains("Allocated"))
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

            if (vehicle != null)
            {
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

                if (!subdealerHistoryActions.Contains("ReturnRequested"))
                {
                    Add(
                        ret.CreatedDate,
                        UnifiedVehicleStatus.ReturnRequested,
                        $"{holderName} — {ret.ReturnReason}",
                        accountLabel,
                        holderName,
                        orderNumber);
                }

                if (!ret.ProcessedDate.HasValue) continue;

                if (ret.Status == 1 && !subdealerHistoryActions.Contains("ReturnApproved"))
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
                else if (ret.Status == 2 && !subdealerHistoryActions.Contains("ReturnRejected"))
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

            if (!subdealerHistoryActions.Contains("Allocated") && !subdealerHistoryActions.Contains("Reassigned"))
            {
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
            }

            foreach (var log in auditLogs.Where(a =>
                         a.Action.Equals("AdminCorrection", StringComparison.OrdinalIgnoreCase)))
            {
                var description = !string.IsNullOrWhiteSpace(log.Remarks)
                    ? log.Remarks
                    : "Admin vehicle correction.";
                Add(
                    log.CreatedDate,
                    vehicle.Status,
                    description,
                    log.UserId > 0 && users.TryGetValue(log.UserId, out var admin)
                        ? admin.GetFullName()
                        : "Admin",
                    ResolveSubdealerName(vehicle.SubdealerId),
                    primaryOrderNumber);
            }

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);

            if (booking != null)
            {
                var bookingSubdealer = ResolveSubdealerName(booking.SubdealerId);
                var customer = booking.CustomerName;

                if (!subdealerHistoryActions.Contains("BookedToCustomer"))
                {
                    Add(
                        booking.SubmittedDate,
                        UnifiedVehicleStatus.BookedToCustomer,
                        $"Customer {customer} ({booking.CustomerMobile}) at {bookingSubdealer}.",
                        bookingSubdealer,
                        ResolveDealershipName(booking.SubdealerId),
                        primaryOrderNumber);
                }

                if (!subdealerHistoryActions.Contains("PaperReceived"))
                    AddBookingMilestone(booking.PaperReceivedDate, UnifiedVehicleStatus.PaperReceived, bookingSubdealer, primaryOrderNumber);
                if (!subdealerHistoryActions.Contains("Invoiced"))
                    AddBookingMilestone(booking.InvoiceDate, UnifiedVehicleStatus.Invoiced, bookingSubdealer, primaryOrderNumber);
                if (!subdealerHistoryActions.Contains("InsuranceCreated"))
                    AddBookingMilestone(booking.InsuranceDate, UnifiedVehicleStatus.InsuranceCreated, bookingSubdealer, primaryOrderNumber);
                if (!subdealerHistoryActions.Contains("RtoRequested"))
                    AddBookingMilestone(booking.AgentDate, UnifiedVehicleStatus.RtoRequested, bookingSubdealer, primaryOrderNumber);
                if (!subdealerHistoryActions.Contains("Registered") && !subdealerHistoryActions.Contains("NumberPlateReceived"))
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
                    && !string.IsNullOrWhiteSpace(booking.SubsidyId)
                    && !subdealerHistoryActions.Contains("SubsidyIdCreated")
                    && !subdealerHistoryActions.Contains("SubsidyDocsSubmitted")
                    && !subdealerHistoryActions.Contains("SubsidyDocsUpdated"))
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

                if (vehicle.Status >= UnifiedVehicleStatus.Delivered
                    && !subdealerHistoryActions.Contains("Delivered"))
                {
                    var deliveredAt = vehicle.DeliveryDate.HasValue
                        ? DateTime.SpecifyKind(vehicle.DeliveryDate.Value, DateTimeKind.Utc)
                        : LatestDate(
                            booking.NumberPlateReceivedDate,
                            latestMilestoneDate,
                            booking.ModifiedDate) ?? booking.ModifiedDate;

                    Add(
                        deliveredAt,
                        UnifiedVehicleStatus.Delivered,
                        vehicle.DeliveryDate.HasValue
                            ? $"Delivered to {customer} on {vehicle.DeliveryDate:yyyy-MM-dd}."
                            : $"Delivered to {customer}.",
                        bookingSubdealer,
                        ResolveDealershipName(booking.SubdealerId),
                        primaryOrderNumber);
                }
            }

            if (vehicle.Status >= UnifiedVehicleStatus.Delivered && booking == null
                && !subdealerHistoryActions.Contains("Delivered"))
            {
                var deliveredAt = vehicle.DeliveryDate.HasValue
                    ? DateTime.SpecifyKind(vehicle.DeliveryDate.Value, DateTimeKind.Utc)
                    : vehicle.ModifiedDate;
                Add(
                    deliveredAt,
                    UnifiedVehicleStatus.Delivered,
                    vehicle.DeliveryDate.HasValue
                        ? $"Delivered on {vehicle.DeliveryDate:yyyy-MM-dd}."
                        : "Vehicle delivered.",
                    ResolveSubdealerName(vehicle.SubdealerId),
                    ResolveDealershipName(vehicle.SubdealerId),
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
                .OrderBy(r => r.OccurredAt)
                .ThenBy(r => StatusSort(r.StatusValue))
                .Select((r, index) =>
                {
                    statusMap.TryGetValue(r.StatusValue, out var st);
                    var local = ToIndiaTime(r.OccurredAt);

                    return new VehicleChassisHistoryEventDto
                    {
                        Step = index + 1,
                        OccurredAt = r.OccurredAt,
                        OccurredAtLocal = local,
                        StatusValue = r.StatusValue,
                        StatusBadgeClass = st?.BadgeClass,
                        Title = StatusName(r.StatusValue),
                        Description = string.IsNullOrWhiteSpace(r.Description) ? "—" : r.Description.Trim(),
                        Actor = r.Actor,
                        Location = r.Location,
                        OrderNumber = r.OrderNumber
                    };
                })
                .ToList();

            var modelId = vehicle?.ModelId ?? master!.ModelId;
            var colorId = vehicle?.ColorId ?? master!.ColorId;
            var currentStatusValue = vehicle?.Status ?? UnifiedVehicleStatus.Submitted;
            statusMap.TryGetValue(currentStatusValue, out var currentStatus);

            return new VehicleChassisHistoryDto
            {
                VehicleId = vehicle?.VehicleId ?? 0,
                ChassisNumber = chassis,
                ModelName = models.TryGetValue(modelId, out var model) ? model.ModelName : $"Model #{modelId}",
                ColorName = colors.TryGetValue(colorId, out var color) ? color.ColorName : $"Color #{colorId}",
                CurrentStatus = currentStatusValue,
                CurrentStatusName = currentStatus?.StatusName,
                CurrentHolder = vehicle?.SubdealerId.HasValue == true
                    ? ResolveSubdealerName(vehicle.SubdealerId)
                    : master?.IsAllocated == true ? "Allocated" : "Dealer Stock",
                Events = combined
            };
        }

        private static JsonDocument? TryParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonDocument.Parse(json); }
            catch { return null; }
        }
    }
}

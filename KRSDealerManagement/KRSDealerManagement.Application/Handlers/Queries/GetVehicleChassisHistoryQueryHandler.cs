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

        private static string FormatSubdealerHistoryDescription(string action, string? remarks)
        {
            var note = remarks?.Trim();
            return action switch
            {
                "ReturnApproved" => string.IsNullOrWhiteSpace(note)
                    ? "Returned to dealer stock."
                    : $"Returned to dealer stock — {note}",
                "ReturnRequested" => string.IsNullOrWhiteSpace(note)
                    ? "Return requested by subdealer."
                    : $"Return requested — {note}",
                "Allocated" => string.IsNullOrWhiteSpace(note)
                    ? "Allocated to subdealer."
                    : $"Allocated — {note}",
                _ => string.IsNullOrWhiteSpace(note) ? action : $"{action} — {note}"
            };
        }

        private static List<RawEvent> DeduplicateRawEvents(List<RawEvent> events)
        {
            return events
                .GroupBy(e => (
                    new DateTime(
                        e.OccurredAt.Year,
                        e.OccurredAt.Month,
                        e.OccurredAt.Day,
                        e.OccurredAt.Hour,
                        e.OccurredAt.Minute,
                        0,
                        e.OccurredAt.Kind),
                    e.StatusValue))
                .Select(g => g.OrderByDescending(ScoreRawEvent).First())
                .ToList();
        }

        private static int ScoreRawEvent(RawEvent e)
        {
            var description = e.Description ?? "";
            var score = description.Length;
            if (description.Contains("Chassis ", StringComparison.OrdinalIgnoreCase)) score += 100;
            if (description.Contains("order ORD-", StringComparison.OrdinalIgnoreCase)) score += 80;
            if (description.StartsWith("Returned to dealer stock", StringComparison.OrdinalIgnoreCase)) score += 60;
            if (description.StartsWith("Order ORD-", StringComparison.OrdinalIgnoreCase)) score += 40;
            if (!string.IsNullOrWhiteSpace(e.OrderNumber)) score += 20;
            return score;
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
            var master = await _unitOfWork.VehicleMasters.GetByChassisAsync(chassis);

            var relatedVehicles = vehicles
                .Where(v =>
                    (master != null && v.VehicleMasterId == master.VehicleMasterId)
                    || (!UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber)
                        && string.Equals(v.ChassisNumber?.Trim(), chassis, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Active lifecycle row = current truth; superseded rows remain in timeline only.
            var vehicle = master != null
                ? VehicleLifecycleHelper.GetActiveRowForMaster(relatedVehicles, master.VehicleMasterId)
                : VehicleLifecycleHelper.FilterActiveLifecycle(relatedVehicles)
                    .OrderByDescending(v => v.ModifiedDate)
                    .ThenByDescending(v => v.VehicleId)
                    .FirstOrDefault()
                ?? relatedVehicles
                    .OrderByDescending(v => v.ModifiedDate)
                    .ThenByDescending(v => v.VehicleId)
                    .FirstOrDefault();

            if (vehicle == null && master == null)
                return null;

            if (master == null && vehicle?.VehicleMasterId > 0)
                master = await _unitOfWork.VehicleMasters.GetByIdAsync(vehicle.VehicleMasterId);

            var relatedVehicleIds = relatedVehicles.Select(v => v.VehicleId).ToHashSet();

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

            var masterMap = master != null
                ? new Dictionary<int, VehicleMaster> { [master.VehicleMasterId] = master }
                : new Dictionary<int, VehicleMaster>();

            string ResolveShowroomLabel(int? orderSubdealerOrgOrUserId = null, int? accountSubdealerUserId = null)
                => DealershipLocationHelper.ResolveShowroomLabel(
                    vehicle,
                    orderSubdealerOrgOrUserId,
                    accountSubdealerUserId,
                    masterMap,
                    dealerships,
                    userOrgRoles,
                    orgs);

            string ResolveOwnShowroomLabel()
            {
                if (master == null || !master.WarrantyOnly)
                    return "";

                var ownShowroom = orgs.Values.FirstOrDefault(o =>
                    o.DealershipId == master.DealershipId && o.OwnShowroom && o.IsActive);
                if (ownShowroom != null)
                {
                    var location = string.IsNullOrWhiteSpace(ownShowroom.Location) ? "" : $" ({ownShowroom.Location})";
                    return $"{ownShowroom.SubDealerName}{location}";
                }

                return dealerships.TryGetValue(master.DealershipId, out var dealer)
                    ? dealer.DealershipName
                    : "Own Showroom";
            }

            string ResolveSubdealerName(int? subdealerOrgOrUserId)
            {
                if (master?.WarrantyOnly == true)
                    return ResolveOwnShowroomLabel();

                if (!subdealerOrgOrUserId.HasValue || subdealerOrgOrUserId.Value <= 0)
                    return "Dealer Stock";

                return SubdealerOrgService.ResolveDisplayName(subdealerOrgOrUserId, userOrgRoles, orgs, users);
            }

            string ResolveDealershipName(int? subdealerOrgOrUserId)
            {
                if (master?.WarrantyOnly == true
                    && dealerships.TryGetValue(master.DealershipId, out var masterDealer))
                {
                    return masterDealer.DealershipName;
                }

                var dealershipId = DealershipLocationHelper.ResolveDealershipIdFromSubdealerOrgOrUser(
                    subdealerOrgOrUserId, orgs, userOrgRoles);
                if (dealershipId.HasValue && dealerships.TryGetValue(dealershipId.Value, out var dealer))
                    return dealer.DealershipName;

                return ResolveShowroomLabel();
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

            string? primaryOrderNumber = vehicle?.PurchaseOrderId is int primaryPoId
                && ordersById.TryGetValue(primaryPoId, out var primaryOrder)
                ? primaryOrder.OrderNumber
                : null;

            var orderItems = (await _unitOfWork.PurchaseOrderItems.GetAllAsync()).ToList();
            var approvedPoVehicleIds = orderItems
                .Where(i => i.Status == 1
                    && i.VehicleId.HasValue
                    && relatedVehicleIds.Contains(i.VehicleId.Value))
                .Select(i => i.VehicleId!.Value)
                .ToHashSet();

            foreach (var related in relatedVehicles.Where(rv => rv.PurchaseOrderId.HasValue))
            {
                if (!ordersById.TryGetValue(related.PurchaseOrderId!.Value, out var order))
                    continue;

                var subdealer = ResolveSubdealerName(order.SubdealerId);
                var cyclePrefix = relatedVehicles.Count > 1 ? $"[Cycle #{related.VehicleId}] " : "";

                Add(
                    order.CreatedDate,
                    UnifiedVehicleStatus.Submitted,
                    $"{cyclePrefix}Order {order.OrderNumber} — {subdealer}.",
                    subdealer,
                    ResolveDealershipName(order.SubdealerId),
                    order.OrderNumber);

                var item = orderItems
                    .Where(i => i.PurchaseOrderId == order.OrderId && i.VehicleId == related.VehicleId)
                    .OrderByDescending(i => i.ApprovedDate ?? i.CreatedDate)
                    .FirstOrDefault();

                if (item != null && item.Status == 1)
                {
                    Add(
                        item.ApprovedDate ?? order.ApprovedDate ?? order.CreatedDate,
                        UnifiedVehicleStatus.ApprovedByDealer,
                        $"{cyclePrefix}Chassis {chassis} allocated to {subdealer} (order {order.OrderNumber}).",
                        "Dealer",
                        ResolveDealershipName(order.SubdealerId),
                        order.OrderNumber);
                }
            }

            var seenSubdealerHistoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var related in relatedVehicles)
            {
                foreach (var h in await _unitOfWork.SubdealerVehicleHistories.GetBySubdealerVehicleIdAsync(related.VehicleId))
                {
                    if (h.Action.Equals("Allocated", StringComparison.OrdinalIgnoreCase)
                        && approvedPoVehicleIds.Contains(related.VehicleId))
                        continue;

                    var dedupeKey = $"{related.VehicleId}|{h.Action}|{h.CreatedDate:yyyy-MM-dd HH:mm:ss}|{h.Remarks}";
                    if (!seenSubdealerHistoryKeys.Add(dedupeKey))
                        continue;

                    subdealerHistoryActions.Add(h.Action);
                    var actor = h.UserId.HasValue && users.TryGetValue(h.UserId.Value, out var u)
                        ? u.GetFullName()
                        : "Staff";
                    var status = VehicleHistoryHelper.ActionToStatus(h.Action) ?? related.Status;
                    var holderAtTime = ResolveSubdealerName(related.SubdealerId);
                    var detail = FormatSubdealerHistoryDescription(h.Action, h.Remarks);
                    if (relatedVehicles.Count > 1)
                        detail = $"[Cycle #{related.VehicleId}] {detail}";
                    Add(h.CreatedDate, status, detail, actor, holderAtTime, null);
                }
            }

            if (master != null)
            {
                foreach (var h in await _unitOfWork.VehicleMasters.GetHistoryAsync(master.VehicleMasterId))
                {
                    if (h.Action.Equals("Allocated", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (h.Action.Equals("Returned", StringComparison.OrdinalIgnoreCase)
                        && subdealerHistoryActions.Contains("ReturnApproved"))
                        continue;

                    var actor = h.UserId.HasValue && users.TryGetValue(h.UserId.Value, out var u)
                        ? u.GetFullName()
                        : "Staff";
                    var status = VehicleHistoryHelper.ActionToStatus(h.Action) ?? UnifiedVehicleStatus.Submitted;
                    var isReturnedToStock = h.Action.Equals("Returned", StringComparison.OrdinalIgnoreCase);
                    var location = master.WarrantyOnly
                        ? ResolveDealershipName(null)
                        : isReturnedToStock ? ResolveShowroomLabel() : "Dealer Stock";
                    var description = isReturnedToStock
                        ? FormatSubdealerHistoryDescription("ReturnApproved", h.Remarks)
                        : (string.IsNullOrWhiteSpace(h.Remarks) ? h.Action : $"{h.Action} — {h.Remarks}");
                    Add(h.CreatedDate, status, description, actor, location, null);
                }
            }

            if (vehicle != null && relatedVehicleIds.Count > 0)
            {
            var returns = (await _unitOfWork.ReturnRequests.GetAllAsync())
                .Where(r => relatedVehicleIds.Contains(r.VehicleId))
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
                    var showroomLabel = ResolveShowroomLabel(retOrder?.SubdealerId, account?.SubdealerId);
                    Add(
                        ret.ProcessedDate.Value,
                        UnifiedVehicleStatus.ReturnApproved,
                        $"Refund ₹{ret.RefundAmount:N2} to {accountLabel}. Moved to {showroomLabel}.",
                        ret.ProcessedBy.HasValue && users.TryGetValue(ret.ProcessedBy.Value, out var admin)
                            ? admin.GetFullName()
                            : "Dealer",
                        showroomLabel,
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
                .Where(a => a.EntityType == "Vehicle" && relatedVehicleIds.Contains(a.EntityId))
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

            var combined = DeduplicateRawEvents(raw)
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

            var currentBooking = vehicle != null
                ? (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId)
                : null;

            var currentHolder = vehicle?.SubdealerId.HasValue == true
                ? ResolveSubdealerName(vehicle.SubdealerId)
                : currentStatusValue == UnifiedVehicleStatus.Delivered
                    ? ResolveSubdealerName(currentBooking?.SubdealerId)
                    : master?.IsAllocated == true ? "Allocated" : "Dealer Stock";

            var currentLocation = currentStatusValue == UnifiedVehicleStatus.Delivered
                ? currentHolder
                : vehicle?.SubdealerId.HasValue == true
                    ? currentHolder
                    : ResolveShowroomLabel();

            string? currentSummary;
            if (vehicle == null)
            {
                currentSummary = master?.IsAllocated == true
                    ? "On dealer master stock."
                    : "Master record only — not allocated.";
            }
            else if (currentStatusValue == UnifiedVehicleStatus.Delivered)
            {
                var customer = WarrantyOnlyVehicleFlowHelper.DisplayCustomerValue(currentBooking?.CustomerName)
                    ?? "customer";
                var deliveredOn = vehicle.DeliveryDate?.ToString("dd-MMM-yyyy") ?? "—";
                currentSummary = $"Delivered to {customer} on {deliveredOn} under {currentHolder}.";
            }
            else if (!vehicle.SubdealerId.HasValue)
            {
                currentSummary = currentStatusValue == UnifiedVehicleStatus.ReturnApproved
                    ? $"At dealer stock (returned) — available for re-allocation at {currentLocation}."
                    : $"At dealer stock — {currentStatus?.StatusName ?? currentStatusValue.ToString()} at {currentLocation}.";
            }
            else
            {
                currentSummary = $"With {currentHolder} — status: {currentStatus?.StatusName ?? currentStatusValue.ToString()}.";
            }

            if (relatedVehicles.Count > 1)
            {
                currentSummary += $" Showing {relatedVehicles.Count} lifecycle cycle(s); current record #{vehicle!.VehicleId}.";
            }

            return new VehicleChassisHistoryDto
            {
                VehicleId = vehicle?.VehicleId ?? 0,
                ChassisNumber = chassis,
                ModelName = models.TryGetValue(modelId, out var model) ? model.ModelName : $"Model #{modelId}",
                ColorName = colors.TryGetValue(colorId, out var color) ? color.ColorName : $"Color #{colorId}",
                CurrentStatus = currentStatusValue,
                CurrentStatusName = currentStatus?.StatusName,
                CurrentHolder = currentHolder,
                CurrentLocation = currentLocation,
                CurrentSummary = currentSummary,
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

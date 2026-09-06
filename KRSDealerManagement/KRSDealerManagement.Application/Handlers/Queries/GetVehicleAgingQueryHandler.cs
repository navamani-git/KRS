using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehicleAgingQueryHandler : IRequestHandler<GetVehicleAgingQuery, IEnumerable<VehicleAgingRowDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehicleAgingQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<VehicleAgingRowDto>> Handle(GetVehicleAgingQuery request, CancellationToken cancellationToken)
        {
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var bookingsByVehicle = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .GroupBy(b => b.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
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
                    return VehicleAgingCalculator.ShouldAppearOnAgingScreen(
                        v.Status,
                        v.SubdealerId,
                        booking?.SubsidyCompletedApproved == true);
                })
                .Select(v =>
                {
                    bookingsByVehicle.TryGetValue(v.VehicleId, out var booking);
                    users.TryGetValue(v.SubdealerId ?? 0, out var user);
                    models.TryGetValue(v.ModelId, out var model);
                    colors.TryGetValue(v.ColorId, out var color);

                    var purchase = v.AllocatedDate ?? v.CreatedDate;
                    var booked = booking?.SubmittedDate;
                    var paper = booking?.PaperReceivedDate;
                    var invoice = booking?.InvoiceDate;
                    var insurance = booking?.InsuranceDate;
                    var agent = booking?.AgentDate;
                    var registration = booking?.RegistrationDate;
                    var subsidyIdDate = !string.IsNullOrWhiteSpace(booking?.SubsidyId)
                        ? booking!.SubsidyIdDate ?? booking.ModifiedDate
                        : (DateTime?)null;
                    var approveDate = booking?.SubsidyCompletedApproved == true
                        ? booking.SubsidyCompletedApprovedDate
                        : null;

                    var chassis = UnifiedVehicleStatus.IsPlaceholderChassis(v.ChassisNumber)
                        ? "-"
                        : (v.ChassisNumber ?? "-");

                    return new VehicleAgingRowDto
                    {
                        VehicleId = v.VehicleId,
                        VehicleBookingId = booking?.VehicleBookingId,
                        ChassisNumber = chassis,
                        ModelName = model?.ModelName ?? $"Model #{v.ModelId}",
                        ColorName = color?.ColorName ?? $"Color #{v.ColorId}",
                        SubdealerId = v.SubdealerId!.Value,
                        SubdealerName = user?.GetFullName() ?? "Unknown",
                        PurchaseDate = purchase,
                        BookedDate = booked,
                        BookedAging = VehicleAgingCalculator.CalendarDays(purchase, booked),
                        PaperReceivedDate = paper,
                        PaperReceivedAging = booked.HasValue
                            ? VehicleAgingCalculator.CalendarDays(booked, paper)
                            : null,
                        InvoiceDate = invoice,
                        InvoiceAging = paper.HasValue
                            ? VehicleAgingCalculator.CalendarDays(paper, invoice)
                            : null,
                        InsuranceDate = insurance,
                        InsuranceAging = invoice.HasValue
                            ? VehicleAgingCalculator.CalendarDays(invoice, insurance)
                            : null,
                        AgentDate = agent,
                        AgentAging = invoice.HasValue
                            ? VehicleAgingCalculator.CalendarDays(invoice, agent)
                            : null,
                        RegistrationDate = registration,
                        RegistrationAging = agent.HasValue
                            ? VehicleAgingCalculator.CalendarDays(agent, registration)
                            : null,
                        SubsidyAging = invoice.HasValue
                            ? VehicleAgingCalculator.CalendarDays(invoice, subsidyIdDate)
                            : null,
                        SubsidyDocumentsAging = subsidyIdDate.HasValue
                            ? VehicleAgingCalculator.CalendarDays(subsidyIdDate, approveDate)
                            : null
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
                    || r.ModelName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.ColorName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.VehicleId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return rows
                .OrderByDescending(r => r.BookedAging ?? r.SubsidyDocumentsAging ?? 0)
                .ThenBy(r => r.SubdealerName)
                .ThenBy(r => r.ChassisNumber)
                .ToList();
        }
    }
}

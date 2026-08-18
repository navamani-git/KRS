using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetCommissionPreviewQueryHandler : IRequestHandler<GetCommissionPreviewQuery, IEnumerable<CommissionPreviewRowDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommissionRateService _commissionRates;
        private readonly IStatusLookupService _statuses;

        public GetCommissionPreviewQueryHandler(
            IUnitOfWork unitOfWork,
            ICommissionRateService commissionRates,
            IStatusLookupService statuses)
        {
            _unitOfWork = unitOfWork;
            _commissionRates = commissionRates;
            _statuses = statuses;
        }

        public async Task<IEnumerable<CommissionPreviewRowDto>> Handle(GetCommissionPreviewQuery request, CancellationToken cancellationToken)
        {
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync())
                .Where(v => v.SubdealerId == request.SubdealerId)
                .ToList();
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .Where(b => b.InvoiceDate.HasValue && b.RegistrationDate.HasValue)
                .ToDictionary(b => b.VehicleId);
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var commissions = (await _unitOfWork.Commissions.GetAllAsync())
                .Where(c => c.SubdealerId == request.SubdealerId)
                .ToList();
            var statusMap = await _statuses.GetMapAsync(StatusCategories.Commission);

            DateTime? from = request.FromDate?.Date;
            DateTime? toExclusive = request.ToDate?.Date.AddDays(1);

            var rows = new List<CommissionPreviewRowDto>();

            foreach (var vehicle in vehicles)
            {
                if (!bookings.TryGetValue(vehicle.VehicleId, out var booking)
                    || !booking.InvoiceDate.HasValue
                    || !booking.RegistrationDate.HasValue)
                    continue;

                var invoice = booking.InvoiceDate.Value.Date;
                var registration = booking.RegistrationDate.Value.Date;
                if (from.HasValue && invoice < from.Value)
                    continue;
                if (toExclusive.HasValue && invoice >= toExclusive.Value)
                    continue;

                var existing = commissions
                    .Where(c => c.VehicleId == vehicle.VehicleId
                        && c.Month == invoice.Month
                        && c.Year == invoice.Year
                        && c.Status != (int)CommissionStatusEnum.Rejected)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefault();

                var isSubmitted = existing != null;
                if (request.PendingOnly && isSubmitted)
                    continue;

                var rate = await _commissionRates.GetAmountAsOfAsync(vehicle.ModelId, invoice);
                models.TryGetValue(vehicle.ModelId, out var model);
                colors.TryGetValue(vehicle.ColorId, out var color);

                var statusLabel = "Not Submitted";
                if (existing != null)
                {
                    statusLabel = statusMap.TryGetValue(existing.Status, out var st)
                        ? st.StatusName
                        : existing.Status.ToString();
                }

                rows.Add(new CommissionPreviewRowDto
                {
                    VehicleId = vehicle.VehicleId,
                    ModelId = vehicle.ModelId,
                    ColorId = vehicle.ColorId,
                    ChassisNumber = vehicle.ChassisNumber ?? "",
                    ModelName = model?.ModelName ?? "Unknown",
                    ColorName = color?.ColorName ?? "Unknown",
                    InvoiceDate = invoice,
                    RegistrationDate = registration,
                    Month = invoice.Month,
                    Year = invoice.Year,
                    ApplicableRate = rate,
                    CommissionStatus = statusLabel,
                    CommissionId = existing?.CommissionId,
                    SubmittedAmount = existing?.CommissionAmount
                });
            }

            return rows.OrderByDescending(r => r.InvoiceDate).ThenBy(r => r.ChassisNumber).ToList();
        }
    }
}

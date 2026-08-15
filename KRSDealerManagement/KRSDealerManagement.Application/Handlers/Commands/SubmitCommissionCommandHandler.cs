using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Enums;
using System.Text.Json;

namespace KRSDealerManagement.Application.Handlers.Commands
{
    public class SubmitCommissionCommandHandler : IRequestHandler<SubmitCommissionCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IAuditService _auditService;

        public SubmitCommissionCommandHandler(IUnitOfWork unitOfWork, IMediator mediator, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _auditService = auditService;
        }

        public async Task<int> Handle(SubmitCommissionCommand request, CancellationToken cancellationToken)
        {
            var chassis = request.ChassisNumber?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(chassis))
                throw new InvalidOperationException("Chassis number is required.");

            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToList();
            var vehicle = vehicles.FirstOrDefault(v =>
                string.Equals(v.ChassisNumber?.Trim(), chassis, StringComparison.OrdinalIgnoreCase));
            if (vehicle == null)
                throw new InvalidOperationException("Chassis number not found.");

            if (!vehicle.SubdealerId.HasValue || vehicle.SubdealerId.Value != request.SubdealerId)
                throw new InvalidOperationException("This chassis is not allocated to your account.");

            if (request.ModelId > 0 && vehicle.ModelId != request.ModelId)
                throw new InvalidOperationException("Chassis does not match the selected vehicle model.");

            if (request.ColorId > 0 && vehicle.ColorId != request.ColorId)
                throw new InvalidOperationException("Chassis does not match the selected color.");

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
            if (booking == null || !booking.InvoiceDate.HasValue)
                throw new InvalidOperationException("Commission can only be submitted after the vehicle is invoiced by the dealer.");

            var existing = (await _unitOfWork.Commissions.GetAllAsync())
                .Any(c => c.VehicleId == vehicle.VehicleId
                    && c.Month == request.Month
                    && c.Year == request.Year
                    && c.SubdealerId == request.SubdealerId
                    && c.Status != (int)CommissionStatusEnum.Rejected);
            if (existing)
                throw new InvalidOperationException($"Commission already submitted for this chassis for {request.Year}-{request.Month:D2}.");

            var accounts = await _mediator.Send(new GetSubdealerAccountsQuery
            {
                SubdealerId = request.SubdealerId,
                IsActive = true
            }, cancellationToken);
            var account = accounts.FirstOrDefault(a =>
                string.Equals(a.AccountType, "Main", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.AccountName, "Main Account", StringComparison.OrdinalIgnoreCase))
                ?? accounts.FirstOrDefault();
            if (account == null)
                throw new InvalidOperationException("No active account found for your profile.");

            var rates = await _mediator.Send(new GetCommissionRatesQuery { ModelId = vehicle.ModelId }, cancellationToken);
            var rate = rates.FirstOrDefault(r => r.IsEffectiveForMonthYear(request.Month, request.Year));
            if (rate == null)
                throw new InvalidOperationException("No commission rate configured for this model and month.");

            if (request.CommissionAmount != rate.CommissionAmount)
                throw new InvalidOperationException($"Commission amount must be ₹{rate.CommissionAmount:N2} as per the configured rate.");

            var commission = new Commission
            {
                AccountId = account.AccountId,
                SubdealerId = request.SubdealerId,
                VehicleId = vehicle.VehicleId,
                Month = request.Month,
                Year = request.Year,
                CommissionAmount = request.CommissionAmount,
                Status = (int)CommissionStatusEnum.Pending,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"Chassis: {chassis}"
                    : request.Notes,
                SubmittedBy = request.SubmittedBy,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            var id = await _unitOfWork.Commissions.AddAsync(commission);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                entityType: "Commission",
                entityId: id,
                action: "Submit",
                userId: request.SubmittedBy,
                userRole: "Subdealer",
                newValue: JsonSerializer.Serialize(new
                {
                    chassis,
                    vehicle.VehicleId,
                    request.Month,
                    request.Year,
                    request.CommissionAmount,
                    InvoiceDate = booking.InvoiceDate?.ToString("yyyy-MM-dd")
                }));

            return id;
        }
    }
}

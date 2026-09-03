using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehicleMastersQueryHandler : IRequestHandler<GetVehicleMastersQuery, IEnumerable<VehicleMasterDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehicleMastersQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<VehicleMasterDto>> Handle(GetVehicleMastersQuery request, CancellationToken cancellationToken)
        {
            var masters = (await _unitOfWork.VehicleMasters.GetAllAsync()).AsEnumerable();
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync()).ToDictionary(d => d.DealershipId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var orgs = (await _unitOfWork.SubDealers.GetAllAsync()).ToDictionary(o => o.SubDealerId);
            var userOrgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync()).ToList();
            var allocationByMasterId = (await _unitOfWork.Vehicles.GetAllAsync())
                .Where(v => v.VehicleMasterId > 0 && v.SubdealerId.HasValue && v.SubdealerId.Value > 0)
                .GroupBy(v => v.VehicleMasterId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(v => v.CreatedDate).First());

            string ResolveAllocatedToName(int? subdealerUserId)
            {
                if (!subdealerUserId.HasValue || subdealerUserId.Value <= 0)
                    return "";

                var assignment = userOrgRoles
                    .Where(a => a.UserId == subdealerUserId.Value && a.IsActive)
                    .OrderByDescending(a => a.IsPrimary)
                    .FirstOrDefault();
                if (assignment?.SubDealerId is int orgId && orgs.TryGetValue(orgId, out var org))
                {
                    var location = string.IsNullOrWhiteSpace(org.Location) ? "" : $" ({org.Location})";
                    return $"{org.SubDealerName}{location}";
                }

                return users.TryGetValue(subdealerUserId.Value, out var user)
                    ? user.GetFullName()
                    : $"Subdealer #{subdealerUserId}";
            }

            if (request.DealershipId.HasValue)
                masters = masters.Where(m => m.DealershipId == request.DealershipId.Value);
            if (request.IsAllocated.HasValue)
                masters = masters.Where(m => m.IsAllocated == request.IsAllocated.Value);
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                masters = masters.Where(m =>
                    m.ChassisNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (m.MotorNo?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return masters
                .OrderByDescending(m => m.ReceivedDate)
                .ThenBy(m => m.ChassisNumber)
                .Select(m =>
                {
                    string? allocatedTo = null;
                    if (m.IsAllocated
                        && allocationByMasterId.TryGetValue(m.VehicleMasterId, out var vehicle))
                    {
                        allocatedTo = ResolveAllocatedToName(vehicle.SubdealerId);
                    }

                    return new VehicleMasterDto
                    {
                        VehicleMasterId = m.VehicleMasterId,
                        DealershipId = m.DealershipId,
                        DealershipName = dealerships.TryGetValue(m.DealershipId, out var d) ? d.DealershipName : "",
                        ChassisNumber = m.ChassisNumber,
                        ModelId = m.ModelId,
                        ModelName = models.TryGetValue(m.ModelId, out var model) ? model.ModelName : "",
                        ColorId = m.ColorId,
                        ColorName = colors.TryGetValue(m.ColorId, out var color) ? color.ColorName : "",
                        MotorNo = m.MotorNo,
                        BatteryNo = m.BatteryNo,
                        ChargerNo = m.ChargerNo,
                        ControllerNo = m.ControllerNo,
                        ConverterNo = m.ConverterNo,
                        AmpereInvoiceNo = m.AmpereInvoiceNo,
                        AmpereInvoiceDate = m.AmpereInvoiceDate,
                        ReceivedDate = m.ReceivedDate,
                        IsAllocated = m.IsAllocated,
                        AllocatedToSubdealerName = allocatedTo,
                        Remarks = m.Remarks,
                        CreatedDate = m.CreatedDate
                    };
                });
        }
    }

    public class GetAvailableVehicleMastersQueryHandler : IRequestHandler<GetAvailableVehicleMastersQuery, IEnumerable<VehicleMasterOptionDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAvailableVehicleMastersQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<VehicleMasterOptionDto>> Handle(GetAvailableVehicleMastersQuery request, CancellationToken cancellationToken)
        {
            var masters = await _unitOfWork.VehicleMasters.GetAvailableByModelColorAsync(
                request.DealershipId, request.ModelId, request.ColorId);

            return masters.Select(m => new VehicleMasterOptionDto
            {
                VehicleMasterId = m.VehicleMasterId,
                ChassisNumber = m.ChassisNumber,
                MotorNo = m.MotorNo,
                BatteryNo = m.BatteryNo,
                ChargerNo = m.ChargerNo,
                ControllerNo = m.ControllerNo,
                ConverterNo = m.ConverterNo,
                AmpereInvoiceNo = m.AmpereInvoiceNo
            });
        }
    }
}

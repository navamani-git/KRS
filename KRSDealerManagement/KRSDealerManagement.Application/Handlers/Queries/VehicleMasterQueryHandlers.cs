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
                .Select(m => new VehicleMasterDto
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
                    ManufacturingYear = m.ManufacturingYear,
                    AmpereInvoiceDate = m.AmpereInvoiceDate,
                    ReceivedDate = m.ReceivedDate,
                    IsAllocated = m.IsAllocated,
                    Remarks = m.Remarks,
                    CreatedDate = m.CreatedDate
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
                ManufacturingYear = m.ManufacturingYear
            });
        }
    }
}

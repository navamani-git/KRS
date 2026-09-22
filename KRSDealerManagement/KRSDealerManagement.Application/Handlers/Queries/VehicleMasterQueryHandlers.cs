using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
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
            var allocationByMasterId = VehicleLifecycleHelper.FilterActiveLifecycle(await _unitOfWork.Vehicles.GetAllAsync())
                .Where(v => v.VehicleMasterId > 0 && v.SubdealerId.HasValue && v.SubdealerId.Value > 0)
                .GroupBy(v => v.VehicleMasterId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(v => v.CreatedDate).First());

            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
            if (dealershipFilter != null)
                masters = masters.Where(m => dealershipFilter.Contains(m.DealershipId));
            if (request.IsAllocated.HasValue)
                masters = masters.Where(m => m.IsAllocated == request.IsAllocated.Value);
            if (request.WarrantyOnly.HasValue)
                masters = masters.Where(m => m.WarrantyOnly == request.WarrantyOnly.Value);
            else
                masters = masters.Where(m => !m.WarrantyOnly);
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
                        allocatedTo = SubdealerOrgService.ResolveDisplayName(
                            vehicle.SubdealerId, userOrgRoles, orgs, users);
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
                        WarrantyOnly = m.WarrantyOnly,
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

    public class GetWarrantyOnlyVehicleEditQueryHandler : IRequestHandler<GetWarrantyOnlyVehicleEditQuery, WarrantyOnlyVehicleEditDto?>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetWarrantyOnlyVehicleEditQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<WarrantyOnlyVehicleEditDto?> Handle(GetWarrantyOnlyVehicleEditQuery request, CancellationToken cancellationToken)
        {
            var master = await _unitOfWork.VehicleMasters.GetByIdAsync(request.VehicleMasterId);
            if (master == null || !master.WarrantyOnly)
                return null;

            var dealershipFilter = DealershipQueryScope.ResolveDealershipIds(request.DealershipId, request.DealershipIds);
            if (dealershipFilter != null && !DealershipQueryScope.MatchesDealership(master.DealershipId, dealershipFilter))
                return null;

            var vehicle = VehicleLifecycleHelper.GetActiveRowForMaster(
                await _unitOfWork.Vehicles.GetAllAsync(),
                master.VehicleMasterId);

            VehicleBooking? booking = null;
            if (vehicle != null)
            {
                booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => b.VehicleId == vehicle.VehicleId);
            }

            return new WarrantyOnlyVehicleEditDto
            {
                VehicleMasterId = master.VehicleMasterId,
                ChassisNumber = master.ChassisNumber,
                ModelId = master.ModelId,
                ColorId = master.ColorId,
                Remarks = master.Remarks,
                CustomerName = WarrantyOnlyVehicleFlowHelper.DisplayCustomerValue(booking?.CustomerName),
                CustomerMobile = WarrantyOnlyVehicleFlowHelper.DisplayCustomerValue(booking?.CustomerMobile),
                SaleDate = booking?.SubmittedDate
            };
        }
    }
}

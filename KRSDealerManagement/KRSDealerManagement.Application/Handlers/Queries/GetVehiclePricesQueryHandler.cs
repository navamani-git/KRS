using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehiclePricesQueryHandler : IRequestHandler<GetVehiclePricesQuery, IEnumerable<VehiclePriceHistoryDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehiclePricesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<VehiclePriceHistoryDto>> Handle(GetVehiclePricesQuery request, CancellationToken cancellationToken)
        {
            var prices = await _unitOfWork.VehiclePriceHistories.GetAllAsync();
            var models = await _unitOfWork.VehicleModels.GetAllAsync();
            var colors = await _unitOfWork.VehicleColors.GetAllAsync();

            // Prices are catalogue entries keyed by ModelId + ColorId (not physical Vehicles)
            var result = from p in prices
                         join m in models on p.ModelId equals m.ModelId
                         join c in colors on p.ColorId equals c.ColorId
                         select new VehiclePriceHistoryDto
                         {
                             PriceHistoryId = p.PriceHistoryId,
                             VehicleId = p.VehicleId ?? 0,
                             ModelId = p.ModelId,
                             ModelName = m.ModelName,
                             ColorId = p.ColorId,
                             ColorName = c.ColorName,
                             Month = p.Month,
                             Year = p.Year,
                             EffectiveFrom = p.EffectiveFrom,
                             EffectiveTo = p.EffectiveTo,
                             Price = p.Price,
                             Notes = p.Notes,
                             CreatedBy = p.CreatedBy,
                             CreatedDate = p.CreatedDate,
                             ModifiedBy = p.ModifiedBy,
                             ModifiedDate = p.ModifiedDate
                         };

            if (request.ModelId.HasValue)
                result = result.Where(p => p.ModelId == request.ModelId.Value);

            if (request.ColorId.HasValue)
                result = result.Where(p => p.ColorId == request.ColorId.Value);

            if (request.Month.HasValue)
                result = result.Where(p => p.Month == request.Month.Value);

            if (request.Year.HasValue)
                result = result.Where(p => p.Year == request.Year.Value);

            return result.OrderByDescending(p => p.Year)
                         .ThenByDescending(p => p.Month)
                         .ThenByDescending(p => p.EffectiveFrom)
                         .ThenBy(p => p.ModelName)
                         .ToList();
        }
    }
}

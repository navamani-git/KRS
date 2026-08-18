using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehicleModelColorMapQueryHandler : IRequestHandler<GetVehicleModelColorMapQuery, Dictionary<int, List<VehicleColorDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehicleModelColorMapQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Dictionary<int, List<VehicleColorDto>>> Handle(
            GetVehicleModelColorMapQuery request, CancellationToken cancellationToken)
        {
            var mappings = (await _unitOfWork.VehicleModelColors.GetAllAsync()).ToList();
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToList();
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToList();

            if (request.ActiveModelsOnly)
                models = models.Where(m => m.IsActive).ToList();

            if (request.ActiveColorsOnly)
                colors = colors.Where(c => c.IsActive).ToList();

            var colorById = colors.ToDictionary(c => c.ColorId);
            var result = new Dictionary<int, List<VehicleColorDto>>();

            foreach (var model in models)
            {
                var mappedColorIds = mappings
                    .Where(m => m.ModelId == model.ModelId)
                    .Select(m => m.ColorId)
                    .Distinct()
                    .Where(colorById.ContainsKey)
                    .OrderBy(id => colorById[id].ColorName)
                    .ToList();

                result[model.ModelId] = mappedColorIds
                    .Select(id => ToDto(colorById[id]))
                    .ToList();
            }

            return result;
        }

        private static VehicleColorDto ToDto(Domain.Entities.VehicleColor color) => new()
        {
            ColorId = color.ColorId,
            ColorName = color.ColorName,
            HexCode = color.HexCode,
            IsActive = color.IsActive,
            CreatedBy = color.CreatedBy,
            CreatedDate = color.CreatedDate,
            ModifiedBy = color.ModifiedBy,
            ModifiedDate = color.ModifiedDate
        };
    }
}

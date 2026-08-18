using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    /// <summary>
    /// Handler for getting single vehicle model by ID
    /// </summary>
    public class GetVehicleModelByIdQueryHandler : IRequestHandler<GetVehicleModelByIdQuery, VehicleModelDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehicleModelByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<VehicleModelDto> Handle(GetVehicleModelByIdQuery request, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.VehicleModels.GetByIdAsync(request.ModelId);

            if (model == null)
                return null;

            var mappedColorIds = (await _unitOfWork.VehicleModelColors.GetColorIdsByModelIdAsync(request.ModelId)).ToList();
            var allColors = await _unitOfWork.VehicleColors.GetAllAsync();
            var mappedColors = allColors
                .Where(c => mappedColorIds.Contains(c.ColorId))
                .OrderBy(c => c.ColorName)
                .Select(c => new VehicleColorDto
                {
                    ColorId = c.ColorId,
                    ColorName = c.ColorName,
                    HexCode = c.HexCode,
                    IsActive = c.IsActive,
                    CreatedBy = c.CreatedBy,
                    CreatedDate = c.CreatedDate,
                    ModifiedBy = c.ModifiedBy,
                    ModifiedDate = c.ModifiedDate
                })
                .ToList();

            return new VehicleModelDto
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                Description = model.Description,
                IsActive = model.IsActive,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
                ModifiedBy = model.ModifiedBy,
                ModifiedDate = model.ModifiedDate,
                MappedColorIds = mappedColorIds,
                MappedColors = mappedColors
            };
        }
    }
}

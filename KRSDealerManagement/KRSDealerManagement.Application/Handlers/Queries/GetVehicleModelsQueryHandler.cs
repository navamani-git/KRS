using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    /// <summary>
    /// Handler for getting all vehicle models with filtering
    /// </summary>
    public class GetVehicleModelsQueryHandler : IRequestHandler<GetVehicleModelsQuery, IEnumerable<VehicleModelDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehicleModelsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<VehicleModelDto>> Handle(GetVehicleModelsQuery request, CancellationToken cancellationToken)
        {
            var models = await _unitOfWork.VehicleModels.GetAllAsync();

            // Apply filters
            if (request.IsActive.HasValue)
            {
                models = models.Where(m => m.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                models = models.Where(m => 
                    m.ModelName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (m.Description != null && m.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            // Map to DTOs
            return models.Select(m => new VehicleModelDto
            {
                ModelId = m.ModelId,
                ModelName = m.ModelName,
                Description = m.Description,
                IsActive = m.IsActive,
                CreatedBy = m.CreatedBy,
                CreatedDate = m.CreatedDate,
                ModifiedBy = m.ModifiedBy,
                ModifiedDate = m.ModifiedDate
            }).OrderBy(m => m.ModelName).ToList();
        }
    }
}

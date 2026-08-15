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

            return new VehicleModelDto
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                Description = model.Description,
                IsActive = model.IsActive,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
                ModifiedBy = model.ModifiedBy,
                ModifiedDate = model.ModifiedDate
            };
        }
    }
}

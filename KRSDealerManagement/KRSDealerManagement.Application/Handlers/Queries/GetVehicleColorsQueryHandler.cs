using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetVehicleColorsQueryHandler : IRequestHandler<GetVehicleColorsQuery, IEnumerable<VehicleColorDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetVehicleColorsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<VehicleColorDto>> Handle(GetVehicleColorsQuery request, CancellationToken cancellationToken)
        {
            var colors = await _unitOfWork.VehicleColors.GetAllAsync();

            if (request.ModelId.HasValue)
            {
                var mappedIds = (await _unitOfWork.VehicleModelColors.GetColorIdsByModelIdAsync(request.ModelId.Value))
                    .ToHashSet();
                colors = colors.Where(c => mappedIds.Contains(c.ColorId));
            }

            if (request.IsActive.HasValue)
                colors = colors.Where(c => c.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                colors = colors.Where(c => c.ColorName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));

            return colors.Select(c => new VehicleColorDto
            {
                ColorId = c.ColorId,
                ColorName = c.ColorName,
                HexCode = c.HexCode,
                IsActive = c.IsActive,
                CreatedBy = c.CreatedBy,
                CreatedDate = c.CreatedDate,
                ModifiedBy = c.ModifiedBy,
                ModifiedDate = c.ModifiedDate
            }).OrderBy(c => c.ColorName).ToList();
        }
    }
}

using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetCommissionRatesQueryHandler : IRequestHandler<GetCommissionRatesQuery, IEnumerable<CommissionRateDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCommissionRatesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CommissionRateDto>> Handle(GetCommissionRatesQuery request, CancellationToken cancellationToken)
        {
            var rates = await _unitOfWork.CommissionRates.GetAllAsync();
            var models = await _unitOfWork.VehicleModels.GetAllAsync();

            var result = from r in rates
                         join m in models on r.ModelId equals m.ModelId
                         select new CommissionRateDto
                         {
                             CommissionRateId = r.CommissionRateId,
                             ModelId = r.ModelId,
                             ModelName = m.ModelName,
                             CommissionAmount = r.CommissionAmount,
                             StartMonth = r.StartMonth,
                             StartYear = r.StartYear,
                             ExpiryMonth = r.ExpiryMonth,
                             ExpiryYear = r.ExpiryYear,
                             Notes = r.Notes,
                             CreatedBy = r.CreatedBy,
                             CreatedDate = r.CreatedDate,
                             ModifiedBy = r.ModifiedBy,
                             ModifiedDate = r.ModifiedDate
                         };

            if (request.ModelId.HasValue)
                result = result.Where(r => r.ModelId == request.ModelId.Value);

            if (request.ActiveOnly == true)
                result = result.Where(r => r.IsActive());

            return result.OrderByDescending(r => r.StartYear)
                         .ThenByDescending(r => r.StartMonth)
                         .ThenBy(r => r.ModelName)
                         .ToList();
        }
    }
}

using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;

namespace KRSDealerManagement.Application.Services
{
    public class CommissionRateService : ICommissionRateService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CommissionRateService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CommissionRate?> GetRateAsOfAsync(int modelId, DateTime asOfDate)
        {
            var asOf = asOfDate.Date;
            return (await _unitOfWork.CommissionRates.GetAllAsync())
                .Where(r => r.ModelId == modelId
                    && r.EffectiveFrom.Date <= asOf
                    && r.EffectiveTo.Date >= asOf)
                .OrderByDescending(r => r.EffectiveFrom)
                .ThenByDescending(r => r.CommissionRateId)
                .FirstOrDefault();
        }

        public async Task<decimal?> GetAmountAsOfAsync(int modelId, DateTime asOfDate)
        {
            var rate = await GetRateAsOfAsync(modelId, asOfDate);
            return rate?.CommissionAmount;
        }
    }
}

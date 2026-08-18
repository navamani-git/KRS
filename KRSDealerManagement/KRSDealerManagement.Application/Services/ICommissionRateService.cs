using KRSDealerManagement.Domain.Entities;

namespace KRSDealerManagement.Application.Services
{
    public interface ICommissionRateService
    {
        Task<CommissionRate?> GetRateAsOfAsync(int modelId, DateTime asOfDate);
        Task<decimal?> GetAmountAsOfAsync(int modelId, DateTime asOfDate);
    }
}

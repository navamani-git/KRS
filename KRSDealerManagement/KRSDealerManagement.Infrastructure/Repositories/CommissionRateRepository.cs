using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    /// <summary>
    /// CommissionRate repository - maps to VehiclePriceHistory table which stores both price and commission info
    /// </summary>
    public class CommissionRateRepository : Repository<CommissionRate>
    {
        public CommissionRateRepository(ApplicationDbContext context)
            : base(context, "CommissionRates", "CommissionRateId") { }
    }
}

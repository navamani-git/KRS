using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class VehicleColorRepository : Repository<VehicleColor>
    {
        public VehicleColorRepository(ApplicationDbContext context) : base(context, "VehicleColors", "ColorId") { }
    }
}

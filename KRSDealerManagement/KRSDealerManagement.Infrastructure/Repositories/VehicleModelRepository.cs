using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Infrastructure.Data;

namespace KRSDealerManagement.Infrastructure.Repositories
{
    public class VehicleModelRepository : Repository<VehicleModel>
    {
        public VehicleModelRepository(ApplicationDbContext context) : base(context, "VehicleModels", "ModelId") { }
    }
}

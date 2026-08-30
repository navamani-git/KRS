using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetVehicleMastersQuery : IRequest<IEnumerable<VehicleMasterDto>>
    {
        public int? DealershipId { get; set; }
        public bool? IsAllocated { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class GetAvailableVehicleMastersQuery : IRequest<IEnumerable<VehicleMasterOptionDto>>
    {
        public int DealershipId { get; set; }
        public int ModelId { get; set; }
        public int ColorId { get; set; }
    }
}

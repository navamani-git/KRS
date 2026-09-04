using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetWarrantyClaimsQuery : IRequest<IEnumerable<WarrantyClaimDto>>
    {
        public int? Status { get; set; }
        public int? DealershipId { get; set; }
        public int? AccountId { get; set; }
        public int? SubdealerUserId { get; set; }
        public string? ClaimType { get; set; }
    }

    public class GetWarrantyClaimDetailQuery : IRequest<WarrantyClaimDetailDto?>
    {
        public int WarrantyClaimId { get; set; }
        public int? AccountId { get; set; }
        public int? DealershipId { get; set; }
        public bool IsSystemAdmin { get; set; }
    }

    public class GetWarrantyChassisLookupQuery : IRequest<WarrantyChassisLookupDto?>
    {
        public int SubdealerUserId { get; set; }
        public string ChassisNo { get; set; } = "";
    }
}

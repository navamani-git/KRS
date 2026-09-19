using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    public class GetWarrantyClaimsQuery : IRequest<IEnumerable<WarrantyClaimDto>>
    {
        public int? Status { get; set; }
        public int? DealershipId { get; set; }
        public List<int>? DealershipIds { get; set; }
        public int? AccountId { get; set; }
        public int? SubdealerUserId { get; set; }
        public string? ClaimType { get; set; }
        public bool ExcludeDraft { get; set; }
        public bool ExcludeComplete { get; set; }
        public bool OnlyComplete { get; set; }
        public DateTime? CompletedFromDate { get; set; }
        public DateTime? CompletedToDate { get; set; }
    }

    public class GetWarrantyClaimDetailQuery : IRequest<WarrantyClaimDetailDto?>
    {
        public int WarrantyClaimId { get; set; }
        public int? AccountId { get; set; }
        public int? DealershipId { get; set; }
        public List<int>? DealershipIds { get; set; }
        public bool IsSystemAdmin { get; set; }
    }

    public class GetWarrantyChassisLookupQuery : IRequest<WarrantyChassisLookupDto?>
    {
        public string ChassisNo { get; set; } = "";
        /// <summary>Optional subdealer login — enriches customer/sale fields from sold vehicles in that org.</summary>
        public int? SubdealerUserId { get; set; }
    }

    public class SearchWarrantyChassisQuery : IRequest<IEnumerable<WarrantyChassisOptionDto>>
    {
        public string? Term { get; set; }
        public int Take { get; set; } = 50;
    }
}

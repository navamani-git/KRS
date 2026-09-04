using MediatR;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Application.Queries
{
    /// <summary>
    /// Get all subdealers with optional filtering
    /// </summary>
    public class GetSubdealersQuery : IRequest<IEnumerable<UserDto>>
    {
        public bool? IsActive { get; set; }
        public string SearchTerm { get; set; }
        /// <summary>Dealership district filter (Karur / Namakkal / Salem).</summary>
        public string? District { get; set; }
        /// <summary>When set, only subdealers under this dealership (via UserOrgRoles).</summary>
        public int? DealershipId { get; set; }
        public Dictionary<string, string>? ColumnFilters { get; set; }
    }
}

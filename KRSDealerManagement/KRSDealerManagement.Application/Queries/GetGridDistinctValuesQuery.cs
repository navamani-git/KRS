using MediatR;

namespace KRSDealerManagement.Application.Queries
{
    public class GetGridDistinctValuesQuery : IRequest<IReadOnlyList<string>>
    {
        public required string GridId { get; set; }
        public required string Column { get; set; }
        public string? Search { get; set; }
        public int? DealershipId { get; set; }
        public int? SubdealerId { get; set; }
        public int? UserId { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public bool BookingPhaseOnly { get; set; }
        public int Limit { get; set; } = 100;
    }
}

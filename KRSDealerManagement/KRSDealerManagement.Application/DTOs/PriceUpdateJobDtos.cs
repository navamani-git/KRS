namespace KRSDealerManagement.Application.DTOs
{
    public class PriceIncreaseApplyResult
    {
        public bool Applied { get; set; }
        public string Status { get; set; } = "";
        public string? Message { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal Delta { get; set; }
        public int? AccountId { get; set; }
        public bool TransactionLogged { get; set; }
    }

    public class PriceUpdateRunResultDto
    {
        public int RunId { get; set; }
        public string Status { get; set; } = "";
        public DateTime AsOfDate { get; set; }
        public int TotalScanned { get; set; }
        public int TotalUpdated { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalErrors { get; set; }
        public string? SummaryMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TriggerSource { get; set; } = "";
    }

    public class PriceUpdateRunListItemDto
    {
        public int RunId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TriggerSource { get; set; } = "";
        public string? TriggeredByName { get; set; }
        public string Status { get; set; } = "";
        public DateTime AsOfDate { get; set; }
        public int TotalScanned { get; set; }
        public int TotalUpdated { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalErrors { get; set; }
        public string? SummaryMessage { get; set; }
    }

    public class PriceUpdateRunDetailDto
    {
        public int RunId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TriggerSource { get; set; } = "";
        public string? TriggeredByName { get; set; }
        public string Status { get; set; } = "";
        public DateTime AsOfDate { get; set; }
        public int TotalScanned { get; set; }
        public int TotalUpdated { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalErrors { get; set; }
        public string? SummaryMessage { get; set; }
        public int TotalLineCount { get; set; }
        public IReadOnlyList<PriceUpdateRunDetailLineDto> Lines { get; set; } = Array.Empty<PriceUpdateRunDetailLineDto>();
    }

    public class PriceUpdateRunDetailLineDto
    {
        public string ChassisNumber { get; set; } = "";
        public int VehicleId { get; set; }
        public bool IsDealerStock { get; set; }
        public int? SubDealerId { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal Delta { get; set; }
        public int? AccountId { get; set; }
        public bool TransactionLogged { get; set; }
        public string Status { get; set; } = "";
        public string? Message { get; set; }
    }
}

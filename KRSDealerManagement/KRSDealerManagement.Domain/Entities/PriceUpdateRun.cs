namespace KRSDealerManagement.Domain.Entities
{
    public class PriceUpdateRun
    {
        public int RunId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string TriggerSource { get; set; } = "";
        public int? TriggeredByUserId { get; set; }
        public string Status { get; set; } = "Running";
        public DateTime AsOfDate { get; set; }
        public int TotalScanned { get; set; }
        public int TotalUpdated { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalErrors { get; set; }
        public string? SummaryMessage { get; set; }
    }

    public class PriceUpdateRunDetail
    {
        public int DetailId { get; set; }
        public int RunId { get; set; }
        public int VehicleId { get; set; }
        public string ChassisNumber { get; set; } = "";
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

namespace KRSDealerManagement.Domain.Entities
{
    public class GstRate
    {
        public int GstRateId { get; set; }
        public decimal RatePercent { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }

    public class GstScreenDefault
    {
        public string ScreenKey { get; set; } = "";
        public int GstRateId { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}

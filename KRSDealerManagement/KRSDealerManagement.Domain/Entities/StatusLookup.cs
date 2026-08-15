namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// DB-backed status master (ORDER, PAYMENT, RETURN, COMMISSION, ORDER_ITEM, VEHICLE).
    /// StatusValue matches the INT Status columns used in transactional tables.
    /// </summary>
    public class StatusLookup
    {
        public int StatusLookupId { get; set; }
        public string Category { get; set; } = "";
        public int StatusValue { get; set; }
        public string StatusCode { get; set; } = "";
        public string StatusName { get; set; } = "";
        public string BadgeClass { get; set; } = "bg-secondary";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

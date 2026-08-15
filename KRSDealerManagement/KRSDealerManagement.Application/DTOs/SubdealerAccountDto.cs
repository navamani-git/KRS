namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Subdealer Account Data Transfer Object
    /// </summary>
    public class SubdealerAccountDto
    {
        public int AccountId { get; set; }
        public int SubdealerId { get; set; }
        public required string SubdealerName { get; set; }
        public required string AccountName { get; set; }
        public required string AccountType { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal ReservedAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetDisplayName() => $"{AccountName} ({AccountType})";
    }
}

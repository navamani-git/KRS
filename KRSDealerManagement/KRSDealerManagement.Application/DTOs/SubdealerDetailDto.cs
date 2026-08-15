namespace KRSDealerManagement.Application.DTOs
{
    public class SubdealerDetailDto
    {
        public int UserId { get; set; }
        public int? SubDealerId { get; set; }
        public int DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public required string Username { get; set; }
        public string? PasswordHash { get; set; }
        public required string SubdealerName { get; set; }
        public required string Location { get; set; }
        public required string Email { get; set; }
        public required string PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? SalesRepMobile { get; set; }
        public string? ServiceRepMobile { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool CanRevealPassword()
        {
            if (string.IsNullOrWhiteSpace(PasswordHash)) return false;
            return !(PasswordHash.StartsWith("AQAA", StringComparison.Ordinal)
                     || PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal));
        }
    }
}

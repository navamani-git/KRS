namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>Business subdealer org (e.g. KPN Motors) with multiple login users.</summary>
    public class SubdealerDetailDto
    {
        public int SubDealerId { get; set; }
        public int DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public required string SubdealerName { get; set; }
        public required string Location { get; set; }
        public required string Email { get; set; }
        public required string PrimaryPhone { get; set; }
        public string? SecondaryPhone { get; set; }
        public string? SalesRepMobile { get; set; }
        public string? ServiceRepMobile { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? PrimaryUserId { get; set; }
        public int? WalletAccountId { get; set; }
        public List<SubdealerLoginDto> Logins { get; set; } = new();
    }
}

namespace KRSDealerManagement.Application.DTOs
{
    public class SubdealerLoginDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public string? DisplayName { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public int PermissionAccountId { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool CanRevealPassword()
        {
            if (string.IsNullOrWhiteSpace(PasswordHash)) return false;
            return !(PasswordHash.StartsWith("AQAA", StringComparison.Ordinal)
                     || PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal));
        }
    }
}

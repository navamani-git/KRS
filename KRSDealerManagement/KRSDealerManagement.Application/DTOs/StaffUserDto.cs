namespace KRSDealerManagement.Application.DTOs
{
    public class StaffUserDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int UserRole { get; set; }
        public int? RoleId { get; set; }
        public required string RoleName { get; set; }
        public int? DealershipId { get; set; }
        public string? DealershipName { get; set; }
        public bool IsActive { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool CanRevealPassword()
        {
            if (string.IsNullOrWhiteSpace(PasswordHash)) return false;
            return !(PasswordHash.StartsWith("AQAA", StringComparison.Ordinal)
                     || PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal));
        }
    }
}

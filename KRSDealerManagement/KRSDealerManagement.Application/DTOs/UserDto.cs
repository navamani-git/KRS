namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// User Data Transfer Object
    /// </summary>
    public class UserDto
    {
        public int UserId { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        /// <summary>Stored login password (plain for admin view when not Identity-hashed).</summary>
        public string? PasswordHash { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int UserRole { get; set; }
        public required string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string GetFullName() => $"{FirstName} {LastName}".Trim();
        public string GetRoleDisplay() => UserRole switch
        {
            1 => "System Admin",
            2 => "Subdealer",
            3 => "Finance Admin",
            4 => "Branch Manager",
            _ => "Unknown"
        };

        /// <summary>True when password can be revealed in admin UI (not an Identity hash).</summary>
        public bool CanRevealPassword()
        {
            if (string.IsNullOrWhiteSpace(PasswordHash)) return false;
            return !(PasswordHash.StartsWith("AQAA", StringComparison.Ordinal)
                     || PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal));
        }
    }
}

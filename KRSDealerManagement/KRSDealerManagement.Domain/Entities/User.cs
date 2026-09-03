using KRSDealerManagement.Shared.Enums;

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// User entity - represents system users (Admin or Subdealers)
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Unique username for login
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Hashed password (never store plaintext)
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User's last name (optional)
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Legacy mirror of role (1=SystemAdmin, 2=Subdealer, 3=Finance, 4=BranchManager).
        /// Source of truth for access is UserOrgRoles + Roles tables.
        /// </summary>
        public int UserRole { get; set; }

        /// <summary>
        /// Optional phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Whether user account is active
        /// Soft-delete: set to false instead of deleting
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Whether this user may download Excel exports.</summary>
        public bool CanExport { get; set; } = true;

        /// <summary>Comma-separated dashboard quick-action keys (null = defaults).</summary>
        public string? QuickActionKeys { get; set; }

        /// <summary>Comma-separated dashboard pill order (null = defaults).</summary>
        public string? DashboardWidgetKeys { get; set; }

        /// <summary>
        /// Account creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get user's full name
        /// </summary>
        public string GetFullName()
        {
            return $"{FirstName} {LastName}".Trim();
        }

        /// <summary>
        /// Check if this is an admin user
        /// </summary>
        public bool IsAdmin()
        {
            return UserRole == (int)UserRoleEnum.Admin;
        }

        /// <summary>
        /// Check if this is a subdealer user
        /// </summary>
        public bool IsSubdealer()
        {
            return UserRole == (int)UserRoleEnum.Subdealer;
        }

        /// <summary>
        /// Get role as enum
        /// </summary>
        public UserRoleEnum GetRole()
        {
            return (UserRoleEnum)UserRole;
        }
    }
}

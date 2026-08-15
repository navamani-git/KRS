namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Represents a business account/unit for a subdealer
    /// Each subdealer can have multiple accounts with separate balances and permissions
    /// </summary>
    public class SubdealerAccount
    {
        /// <summary>
        /// Unique identifier for this account
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Reference to parent subdealer (User)
        /// </summary>
        public int SubdealerId { get; set; }

        /// <summary>
        /// Display name (e.g., "Main Sales", "Fleet Operations")
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Type of account for categorization (e.g., "Sales", "Fleet", "Corporate")
        /// </summary>
        public string AccountType { get; set; }

        /// <summary>
        /// Optional description of account purpose
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether account is active and usable
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Account creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get account identifier for display
        /// </summary>
        public string GetDisplayName()
        {
            return $"{AccountName} ({AccountType})";
        }

        /// <summary>
        /// Check if account is available for use
        /// </summary>
        public bool IsAvailable()
        {
            return IsActive;
        }
    }
}

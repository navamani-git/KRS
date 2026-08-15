namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Defines menu/feature permissions for a specific account
    /// Controls what operations an account can perform
    /// </summary>
    public class AccountPermission
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int PermissionId { get; set; }

        /// <summary>
        /// Reference to the SubdealerAccount this permission applies to
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// System key for menu/feature (e.g., "purchase_orders", "commissions")
        /// Used for permission checks in code
        /// </summary>
        public string MenuKey { get; set; }

        /// <summary>
        /// Human-readable display name (e.g., "Purchase Orders")
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Whether account can access this menu/feature
        /// If false, entire menu hidden from navigation
        /// </summary>
        public bool IsAccessible { get; set; } = false;

        /// <summary>
        /// Whether account can create new items in this feature
        /// </summary>
        public bool CanCreate { get; set; } = false;

        /// <summary>
        /// Whether account can modify existing items
        /// </summary>
        public bool CanEdit { get; set; } = false;

        /// <summary>
        /// Whether account can delete items
        /// </summary>
        public bool CanDelete { get; set; } = false;

        /// <summary>
        /// Whether account can approve/confirm items
        /// Typically for admin accounts only
        /// </summary>
        public bool CanApprove { get; set; } = false;

        /// <summary>
        /// Permission creation timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if account can perform action on this menu
        /// </summary>
        public bool CanPerformAction(string action)
        {
            if (!IsAccessible)
                return false;

            return action switch
            {
                "create" => CanCreate,
                "edit" => CanEdit,
                "delete" => CanDelete,
                "approve" => CanApprove,
                "view" => true, // Can view if accessible
                _ => false
            };
        }

        /// <summary>
        /// Get summary of permissions as readable text
        /// </summary>
        public string GetPermissionsSummary()
        {
            var permissions = new List<string>();
            
            if (!IsAccessible)
                return "No Access";

            permissions.Add("View");
            if (CanCreate) permissions.Add("Create");
            if (CanEdit) permissions.Add("Edit");
            if (CanDelete) permissions.Add("Delete");
            if (CanApprove) permissions.Add("Approve");

            return string.Join(", ", permissions);
        }
    }
}

namespace KRSDealerManagement.Domain.Entities
{
    /// <summary>
    /// Complete audit trail for system actions
    /// Records all Create, Update, Delete, Approve, Reject operations
    /// Used for compliance and troubleshooting
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public int AuditLogId { get; set; }

        /// <summary>
        /// Type of entity affected (e.g., PurchaseOrder, Commission, Vehicle)
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// ID of entity that was modified
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Action performed: Create, Update, Delete, Approve, Reject, Return, etc.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// User who performed the action
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User's role at time of action (Admin, Dealer, Subdealer)
        /// </summary>
        public string UserRole { get; set; }

        /// <summary>
        /// Old value (if applicable) - serialized JSON
        /// For comparison and rollback scenarios
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// New value (if applicable) - serialized JSON
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Admin/user remarks on the action
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// IP address from where action was performed
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Browser/client information
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Action timestamp (UTC)
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get display info for audit entry
        /// </summary>
        public string GetDisplayInfo()
        {
            return $"{EntityType} #{EntityId} | {Action} | By User {UserId} ({UserRole})";
        }

        /// <summary>
        /// Check if this is a modification (not creation or deletion)
        /// </summary>
        public bool IsModification()
        {
            return Action.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                   Action.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
                   Action.Equals("Reject", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if old/new values are available
        /// </summary>
        public bool HasValueChange()
        {
            return !string.IsNullOrEmpty(OldValue) || !string.IsNullOrEmpty(NewValue);
        }

        /// <summary>
        /// Get time difference from creation to now
        /// </summary>
        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - CreatedDate;
        }

        /// <summary>
        /// Format age as readable string
        /// </summary>
        public string GetAgeDisplay()
        {
            var age = GetAge();

            if (age.TotalMinutes < 1)
                return "Just now";
            if (age.TotalHours < 1)
                return $"{(int)age.TotalMinutes}m ago";
            if (age.TotalDays < 1)
                return $"{(int)age.TotalHours}h ago";
            if (age.TotalDays < 30)
                return $"{(int)age.TotalDays}d ago";

            return CreatedDate.ToString("dd/MM/yyyy HH:mm");
        }
    }
}

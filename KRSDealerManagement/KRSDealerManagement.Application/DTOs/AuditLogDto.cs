namespace KRSDealerManagement.Application.DTOs
{
    /// <summary>
    /// Audit Log Data Transfer Object
    /// </summary>
    public class AuditLogDto
    {
        public int AuditLogId { get; set; }
        public required string EntityType { get; set; }
        public int EntityId { get; set; }
        public required string Action { get; set; }
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public required string UserRole { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Remarks { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedDate { get; set; }

        public string GetDisplayInfo()
        {
            return $"{EntityType} #{EntityId} | {Action} | By {UserName} ({UserRole})";
        }

        public bool IsModification()
        {
            return Action.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                   Action.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
                   Action.Equals("Reject", StringComparison.OrdinalIgnoreCase);
        }

        public bool HasValueChange()
        {
            return !string.IsNullOrEmpty(OldValue) || !string.IsNullOrEmpty(NewValue);
        }

        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - CreatedDate;
        }

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

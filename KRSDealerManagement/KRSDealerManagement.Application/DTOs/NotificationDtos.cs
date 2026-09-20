namespace KRSDealerManagement.Application.DTOs
{
    public class AdminNotificationListItemDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string LocationSummary { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string CreatedByName { get; set; } = "";
        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminNotificationDetailDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public bool TargetAllLocations { get; set; }
        public IReadOnlyList<int> DealershipIds { get; set; } = Array.Empty<int>();
        public string LocationSummary { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string CreatedByName { get; set; } = "";
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedByName { get; set; }
        public bool IsActive { get; set; }
        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
        public IReadOnlyList<NotificationRecipientStatusDto> Recipients { get; set; } = Array.Empty<NotificationRecipientStatusDto>();
    }

    public class NotificationRecipientStatusDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string SubDealerName { get; set; } = "";
        public string DealershipName { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
    }

    public class SubdealerNotificationListItemDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string BodyPreview { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
    }

    public class SubdealerNotificationDetailDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}

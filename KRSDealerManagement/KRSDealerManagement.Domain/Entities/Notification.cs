namespace KRSDealerManagement.Domain.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public bool TargetAllLocations { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedByUserId { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int? DeletedByUserId { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class NotificationTarget
    {
        public int NotificationTargetId { get; set; }
        public int NotificationId { get; set; }
        public int DealershipId { get; set; }
    }

    public class NotificationRecipient
    {
        public int NotificationRecipientId { get; set; }
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public int SubDealerId { get; set; }
        public int DealershipId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}

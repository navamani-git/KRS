using KRSDealerManagement.Application.DTOs;
using MediatR;

namespace KRSDealerManagement.Application.Queries
{
    public class GetAdminNotificationsQuery : IRequest<IReadOnlyList<AdminNotificationListItemDto>>
    {
        public bool IncludeInactive { get; set; }
    }

    public class GetAdminNotificationDetailQuery : IRequest<AdminNotificationDetailDto?>
    {
        public int NotificationId { get; set; }
    }

    public class GetSubdealerNotificationsQuery : IRequest<IReadOnlyList<SubdealerNotificationListItemDto>>
    {
        public int UserId { get; set; }
    }

    public class GetSubdealerNotificationDetailQuery : IRequest<SubdealerNotificationDetailDto?>
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
    }

    public class GetUnreadNotificationCountQuery : IRequest<int>
    {
        public int UserId { get; set; }
    }

    public class PreviewNotificationRecipientCountQuery : IRequest<int>
    {
        public bool TargetAllLocations { get; set; }
        public List<int> DealershipIds { get; set; } = new();
    }
}

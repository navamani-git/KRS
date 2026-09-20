using MediatR;

namespace KRSDealerManagement.Application.Commands
{
    public class CreateNotificationCommand : IRequest<int>
    {
        public required string Title { get; set; }
        public required string Body { get; set; }
        public bool TargetAllLocations { get; set; }
        public List<int> DealershipIds { get; set; } = new();
        public int CreatedByUserId { get; set; }
    }

    public class UpdateNotificationCommand : IRequest<bool>
    {
        public int NotificationId { get; set; }
        public required string Title { get; set; }
        public required string Body { get; set; }
        public bool TargetAllLocations { get; set; }
        public List<int> DealershipIds { get; set; } = new();
        public int ModifiedByUserId { get; set; }
    }

    public class DeleteNotificationCommand : IRequest<bool>
    {
        public int NotificationId { get; set; }
        public int DeletedByUserId { get; set; }
    }

    public class MarkNotificationReadCommand : IRequest<bool>
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
    }
}

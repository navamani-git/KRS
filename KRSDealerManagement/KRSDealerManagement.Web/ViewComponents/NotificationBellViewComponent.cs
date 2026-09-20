using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Helpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly IMediator _mediator;

        public NotificationBellViewComponent(IMediator mediator) => _mediator = mediator;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var session = HttpContext.Session;
            if (!SessionHelper.IsAuthenticated(session)
                || !SessionHelper.IsSubdealer(session)
                || !SessionHelper.HasMenuAccess(session, MenuKeys.Notifications))
            {
                return Content(string.Empty);
            }

            var userId = SessionHelper.GetUserId(session);
            if (!userId.HasValue)
                return Content(string.Empty);

            var unreadCount = await _mediator.Send(new GetUnreadNotificationCountQuery { UserId = userId.Value });
            return View(new NotificationBellModel { UnreadCount = unreadCount });
        }
    }

    public class NotificationBellModel
    {
        public int UnreadCount { get; set; }
    }
}

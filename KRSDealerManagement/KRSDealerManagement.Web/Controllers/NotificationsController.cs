using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeMenu(MenuKeys.Notifications, SubdealerOnly = true)]
    public class NotificationsController : Controller
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator) => _mediator = mediator;

        public async Task<IActionResult> Index()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var items = await _mediator.Send(new GetSubdealerNotificationsQuery { UserId = userId.Value });
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            await _mediator.Send(new MarkNotificationReadCommand
            {
                NotificationId = id,
                UserId = userId.Value
            });

            var detail = await _mediator.Send(new GetSubdealerNotificationDetailQuery
            {
                NotificationId = id,
                UserId = userId.Value
            });

            if (detail == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(detail);
        }
    }
}

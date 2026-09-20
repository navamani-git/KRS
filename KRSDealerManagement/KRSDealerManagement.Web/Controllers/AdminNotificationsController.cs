using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.Notifications, StaffOnly = true)]
    public class AdminNotificationsController : Controller
    {
        private readonly IMediator _mediator;

        public AdminNotificationsController(IMediator mediator) => _mediator = mediator;

        public async Task<IActionResult> Index(bool showDeleted = false)
        {
            var items = await _mediator.Send(new GetAdminNotificationsQuery { IncludeInactive = showDeleted });
            ViewBag.ShowDeleted = showDeleted;
            return View(items);
        }

        public async Task<IActionResult> Create()
        {
            await LoadFormViewBagsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string title,
            string body,
            bool targetAllLocations,
            List<int>? dealershipIds)
        {
            await LoadFormViewBagsAsync();
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            try
            {
                var id = await _mediator.Send(new CreateNotificationCommand
                {
                    Title = title ?? "",
                    Body = body ?? "",
                    TargetAllLocations = targetAllLocations,
                    DealershipIds = dealershipIds ?? new List<int>(),
                    CreatedByUserId = userId.Value
                });
                TempData["Success"] = "Notification sent successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Title = title;
                ViewBag.Body = body;
                ViewBag.TargetAllLocations = targetAllLocations;
                ViewBag.SelectedDealershipIds = dealershipIds ?? new List<int>();
                return View();
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var detail = await _mediator.Send(new GetAdminNotificationDetailQuery { NotificationId = id });
            if (detail == null || !detail.IsActive)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadFormViewBagsAsync();
            ViewBag.NotificationId = detail.NotificationId;
            ViewBag.Title = detail.Title;
            ViewBag.Body = detail.Body;
            ViewBag.TargetAllLocations = detail.TargetAllLocations;
            ViewBag.SelectedDealershipIds = detail.DealershipIds.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string title,
            string body,
            bool targetAllLocations,
            List<int>? dealershipIds)
        {
            await LoadFormViewBagsAsync();
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            try
            {
                await _mediator.Send(new UpdateNotificationCommand
                {
                    NotificationId = id,
                    Title = title ?? "",
                    Body = body ?? "",
                    TargetAllLocations = targetAllLocations,
                    DealershipIds = dealershipIds ?? new List<int>(),
                    ModifiedByUserId = userId.Value
                });
                TempData["Success"] = "Notification updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.NotificationId = id;
                ViewBag.Title = title;
                ViewBag.Body = body;
                ViewBag.TargetAllLocations = targetAllLocations;
                ViewBag.SelectedDealershipIds = dealershipIds ?? new List<int>();
                return View();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var detail = await _mediator.Send(new GetAdminNotificationDetailQuery { NotificationId = id });
            if (detail == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(detail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var deleted = await _mediator.Send(new DeleteNotificationCommand
            {
                NotificationId = id,
                DeletedByUserId = userId.Value
            });

            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Notification deleted."
                : "Notification not found.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> PreviewCount(bool targetAllLocations, List<int>? dealershipIds)
        {
            var count = await _mediator.Send(new PreviewNotificationRecipientCountQuery
            {
                TargetAllLocations = targetAllLocations,
                DealershipIds = dealershipIds ?? new List<int>()
            });
            return Json(new { count });
        }

        private async Task LoadFormViewBagsAsync()
        {
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
        }
    }
}

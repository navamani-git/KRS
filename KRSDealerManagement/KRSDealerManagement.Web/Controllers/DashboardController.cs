using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 2, 3, 4)] // All authenticated roles
    public class DashboardController : Controller
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var userRole = SessionHelper.GetUserRole(HttpContext.Session);
            var fullName = SessionHelper.GetFullName(HttpContext.Session);

            if (!userId.HasValue || !userRole.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var query = new GetDashboardSummaryQuery
            {
                IncludeRecentActivities = isAdmin
            };

            if (userRole.Value == 2) // Subdealer
            {
                query.SubdealerId = userId.Value;
            }

            var summary = await _mediator.Send(query);

            ViewBag.FullName = fullName;
            ViewBag.UserRole = userRole.Value;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsSubdealer = userRole.Value == 2;

            return View(summary);
        }
    }
}

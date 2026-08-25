using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;

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
            var isSubdealer = SessionHelper.IsSubdealer(HttpContext.Session);
            var isBranchManager = SessionHelper.IsBranchManager(HttpContext.Session);
            var canViewPayments = isSubdealer
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Payments);
            var canViewOrders = isSubdealer
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Orders);
            var canViewReturns = isSubdealer
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Returns);
            var canViewCommissions = isSubdealer || isAdmin;
            var canViewBookings = isAdmin || isBranchManager
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.VehicleBookings);

            var query = new GetDashboardSummaryQuery
            {
                IncludeRecentActivities = isAdmin,
                IncludePaymentPending = canViewPayments
            };

            if (isSubdealer)
            {
                query.SubdealerId = userId.Value;
            }
            else if (!isAdmin)
            {
                query.DealershipId = SessionHelper.GetDealershipScope(HttpContext.Session);
            }

            var summary = await _mediator.Send(query);

            ViewBag.FullName = fullName;
            ViewBag.UserRole = userRole.Value;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsSubdealer = isSubdealer;
            ViewBag.IsBranchManager = isBranchManager;
            ViewBag.CanViewPendingPayments = canViewPayments;
            ViewBag.CanViewPendingOrders = canViewOrders;
            ViewBag.CanViewPendingReturns = canViewReturns;
            ViewBag.CanViewPendingCommissions = canViewCommissions;
            ViewBag.CanViewBookings = canViewBookings;
            ViewBag.DealershipName = SessionHelper.GetDealershipName(HttpContext.Session);

            return View(summary);
        }
    }
}

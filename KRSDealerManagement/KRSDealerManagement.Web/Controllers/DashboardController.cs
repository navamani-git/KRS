using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 2, 3, 4)] // All authenticated roles
    public class DashboardController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
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
            var canViewBookings = isAdmin
                || SessionHelper.HasAnyBookingStaffMenuAccess(HttpContext.Session)
                || (isSubdealer && SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesBookingStages));
            var canViewShowroomStock = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.ShowroomStock);
            var canViewDealerStock = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.DealerStock);
            var canViewRtoSubsidyProgress = isAdmin
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.BookingSubsidyIdPending)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.BookingSubsidyDocsPending)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.BookingRegistered)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.VehicleBookings)
                || (isSubdealer && SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesBookingStages));

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
            ViewBag.CanViewShowroomStock = canViewShowroomStock;
            ViewBag.CanViewDealerStock = canViewDealerStock;
            ViewBag.CanViewRtoSubsidyProgress = canViewRtoSubsidyProgress;
            ViewBag.DealershipName = SessionHelper.GetDealershipName(HttpContext.Session);

            var showBookingCounts = canViewBookings;
            var widgetContext = new DashboardWidgetsContext
            {
                IsAdmin = isAdmin,
                IsSubdealer = isSubdealer,
                IsBranchManager = isBranchManager,
                CanViewOrders = canViewOrders,
                CanViewReturns = canViewReturns,
                CanViewPayments = canViewPayments,
                CanViewCommissions = canViewCommissions,
                CanViewDealerStock = canViewDealerStock,
                CanViewShowroomStock = canViewShowroomStock,
                ShowBookingCounts = showBookingCounts,
                ShowStaffOnlyBookingStages = isAdmin || isBranchManager,
                CanViewRtoSubsidyProgress = canViewRtoSubsidyProgress
            };

            var widgetCatalog = DashboardWidgets.GetCatalog(widgetContext);
            var savedWidgetKeys = SessionHelper.GetDashboardWidgetKeys(HttpContext.Session);
            if (savedWidgetKeys == null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                savedWidgetKeys = user?.DashboardWidgetKeys;
                if (savedWidgetKeys != null)
                    HttpContext.Session.SetString("DashboardWidgetKeys", savedWidgetKeys);
            }

            var orderedWidgetKeys = DashboardWidgets.ResolveOrder(widgetCatalog, savedWidgetKeys);
            ViewBag.DashboardWidgets = new DashboardWidgetsCustomizeModel
            {
                Catalog = widgetCatalog,
                OrderedKeys = orderedWidgetKeys,
                Sections = DashboardWidgets.GroupBySections(widgetCatalog, orderedWidgetKeys)
            };

            return View(summary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveQuickActions(string[]? quickActions, string? returnUrl)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            string UrlFor(string controller, string action, IReadOnlyDictionary<string, object>? routeValues) =>
                routeValues is { Count: > 0 }
                    ? Url.Action(action, controller, routeValues) ?? "#"
                    : Url.Action(action, controller) ?? "#";

            var catalog = DashboardQuickActions.GetCatalog(HttpContext.Session, UrlFor);
            var allowed = catalog.Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var picked = (quickActions ?? Array.Empty<string>())
                .Where(k => allowed.Contains(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
            if (user == null) return RedirectToAction(nameof(Index));

            user.QuickActionKeys = picked.Count == 0 ? "" : string.Join(",", picked);
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            HttpContext.Session.SetString("QuickActionKeys", user.QuickActionKeys);
            TempData["Success"] = "Quick actions updated.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDashboardWidgets(string[]? widgetOrder, bool? resetOrder)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var isSubdealer = SessionHelper.IsSubdealer(HttpContext.Session);
            var isBranchManager = SessionHelper.IsBranchManager(HttpContext.Session);
            var canViewBookings = isAdmin
                || SessionHelper.HasAnyBookingStaffMenuAccess(HttpContext.Session)
                || (isSubdealer && SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesBookingStages));

            var widgetContext = new DashboardWidgetsContext
            {
                IsAdmin = isAdmin,
                IsSubdealer = isSubdealer,
                IsBranchManager = isBranchManager,
                CanViewOrders = isSubdealer || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Orders),
                CanViewReturns = isSubdealer || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Returns),
                CanViewPayments = isSubdealer || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Payments),
                CanViewCommissions = isSubdealer || isAdmin,
                CanViewDealerStock = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.DealerStock),
                CanViewShowroomStock = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.ShowroomStock),
                ShowBookingCounts = canViewBookings,
                ShowStaffOnlyBookingStages = isAdmin || isBranchManager,
                CanViewRtoSubsidyProgress = isAdmin
                    || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.VehicleBookings)
                    || (isSubdealer && SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesBookingStages))
            };

            var catalog = DashboardWidgets.GetCatalog(widgetContext);
            var allowed = catalog.Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<string> ordered;
            if (resetOrder == true)
            {
                ordered = catalog.Select(c => c.Key).ToList();
            }
            else
            {
                ordered = new List<string>();
                foreach (var key in widgetOrder ?? Array.Empty<string>())
                {
                    if (allowed.Contains(key) && !ordered.Contains(key, StringComparer.OrdinalIgnoreCase))
                        ordered.Add(key);
                }

                foreach (var item in catalog)
                {
                    if (!ordered.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                        ordered.Add(item.Key);
                }
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
            if (user == null) return RedirectToAction(nameof(Index));

            user.DashboardWidgetKeys = resetOrder == true || ordered.Count == 0
                ? null
                : string.Join(",", ordered);
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            if (user.DashboardWidgetKeys == null)
                HttpContext.Session.Remove("DashboardWidgetKeys");
            else
                HttpContext.Session.SetString("DashboardWidgetKeys", user.DashboardWidgetKeys);

            TempData["Success"] = resetOrder == true
                ? "Dashboard layout reset to default."
                : "Dashboard layout updated.";

            return RedirectToAction(nameof(Index));
        }
    }
}

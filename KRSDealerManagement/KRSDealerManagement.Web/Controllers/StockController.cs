using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 4)]
    public class StockController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public StockController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        [AuthorizeMenu(StaffMenuAccess.ShowroomStock)]
        public async Task<IActionResult> Index(
            string? dealershipLocation,
            int? subdealerId,
            string? searchTerm,
            int? page,
            int? pageSize)
        {
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.ShowroomStock);

            var query = new GetShowroomStockQuery
            {
                DealershipLocation = dealershipLocation,
                SubdealerId = subdealerId,
                SearchTerm = searchTerm
            };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, query);

            var stock = GridScreenFilterHelper.ApplyShowroomStock(
                await _mediator.Send(query),
                columnFilters).ToList();
            ViewBag.TotalStockValue = stock.Sum(v => v.CurrentPrice);
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(stock, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync())
                .Where(d => d.IsActive && (!query.DealershipId.HasValue || d.DealershipId == query.DealershipId.Value))
                .OrderBy(d => d.Location ?? d.DealershipName)
                .ToList();

            ViewBag.DealershipLocations = dealerships
                .Select(d => d.Location?.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(l => l)
                .ToList();
            ViewBag.SelectedDealershipLocation = dealershipLocation;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsAdmin = isAdmin;

            var allSubdealersQuery = new GetSubdealersQuery { IsActive = true };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, allSubdealersQuery);
            var allSubdealers = await _mediator.Send(allSubdealersQuery);

            if (!string.IsNullOrWhiteSpace(dealershipLocation))
            {
                var locDealershipIds = dealerships
                    .Where(d => string.Equals(d.Location?.Trim(), dealershipLocation.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.DealershipId)
                    .ToHashSet();
                var orgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                    .Where(r => r.IsActive && r.DealershipId.HasValue && locDealershipIds.Contains(r.DealershipId.Value))
                    .Select(r => r.UserId)
                    .ToHashSet();
                ViewBag.Subdealers = allSubdealers.Where(s => orgRoles.Contains(s.UserId)).ToList();
            }
            else
            {
                ViewBag.Subdealers = allSubdealers.ToList();
            }

            ViewBag.SelectedSubdealerId = subdealerId;

            return View(pageItems);
        }

        [AuthorizeMenu(StaffMenuAccess.ShowroomStock)]
        public async Task<IActionResult> Export(
            string? dealershipLocation,
            int? subdealerId,
            string? searchTerm)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.ShowroomStock);

            var stockQuery = new GetShowroomStockQuery
            {
                DealershipLocation = dealershipLocation,
                SubdealerId = subdealerId,
                SearchTerm = searchTerm
            };
            DealershipScopeWebHelper.ApplyStaffScope(HttpContext.Session, stockQuery);

            var stock = GridScreenFilterHelper.ApplyShowroomStock(
                await _mediator.Send(stockQuery),
                columnFilters).ToList();

            var headers = new[] { "Location", "Subdealer", "Chassis", "Model", "Color", "Order #", "Allocated", "Days in stock", "Price" };
            var rows = stock.Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.DealershipLocation,
                r.SubdealerName,
                r.ChassisNumber,
                r.ModelName,
                r.ColorName,
                r.OrderNumber,
                r.AllocatedDate?.ToString("yyyy-MM-dd"),
                r.DaysInStock,
                r.CurrentPrice
            });
            return ExcelExportHelper.ToFileResult(this, $"showroom_stock_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Showroom Stock");
        }
    }
}

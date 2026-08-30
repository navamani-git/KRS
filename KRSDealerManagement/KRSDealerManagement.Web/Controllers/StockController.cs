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
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.ShowroomStock);

            var query = new GetShowroomStockQuery
            {
                DealershipId = scope,
                DealershipLocation = dealershipLocation,
                SubdealerId = subdealerId,
                SearchTerm = searchTerm
            };

            var stock = GridScreenFilterHelper.ApplyShowroomStock(
                await _mediator.Send(query),
                columnFilters).ToList();
            ViewBag.TotalStockValue = stock.Sum(v => v.CurrentPrice);
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(stock, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync())
                .Where(d => d.IsActive && (!scope.HasValue || d.DealershipId == scope.Value))
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

            var allSubdealers = await _mediator.Send(new GetSubdealersQuery
            {
                IsActive = true,
                DealershipId = scope
            });

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
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.ShowroomStock);

            var stock = GridScreenFilterHelper.ApplyShowroomStock(
                await _mediator.Send(new GetShowroomStockQuery
                {
                    DealershipId = scope,
                    DealershipLocation = dealershipLocation,
                    SubdealerId = subdealerId,
                    SearchTerm = searchTerm
                }),
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

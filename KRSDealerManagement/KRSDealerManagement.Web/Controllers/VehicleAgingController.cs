using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Application.DTOs;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 2, 4)]
    [AuthorizeMenuAny(StaffMenuAccess.VehicleAging, MenuKeys.VehicleAging)]
    public class VehicleAgingController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public VehicleAgingController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(
            string? dealershipLocation,
            int? subdealerId,
            string? searchTerm,
            int? page,
            int? pageSize)
        {
            var isSubdealer = SessionHelper.IsSubdealer(HttpContext.Session);
            ViewBag.IsSubdealerView = isSubdealer;
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.VehicleAging);

            var query = BuildQuery(dealershipLocation, subdealerId, searchTerm, isSubdealer);
            var rows = GridScreenFilterHelper.ApplyVehicleAging(
                await _mediator.Send(query),
                columnFilters).ToList();

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(rows, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            await LoadFiltersAsync(dealershipLocation, subdealerId, searchTerm, isSubdealer);
            return View(pageItems);
        }

        public async Task<IActionResult> Export(
            string? dealershipLocation,
            int? subdealerId,
            string? searchTerm)
        {
            var isSubdealer = SessionHelper.IsSubdealer(HttpContext.Session);
            ViewBag.IsSubdealerView = isSubdealer;
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.VehicleAging);

            var rows = GridScreenFilterHelper.ApplyVehicleAging(
                await _mediator.Send(BuildQuery(dealershipLocation, subdealerId, searchTerm, isSubdealer)),
                columnFilters).ToList();

            var headers = new List<string> { "ID", "Chassis", "Model", "Color" };
            if (!isSubdealer)
                headers.Add("Subdealer");
            headers.AddRange(new[]
            {
                "Booked Ageing",
                "Paper Received Ageing",
                "Invoice Ageing",
                "Insurance Ageing",
                "Agent Ageing",
                "Registration Ageing",
                "Subsidy Ageing",
                "Subsidy Documents Ageing"
            });

            IReadOnlyList<object?> Map(VehicleAgingRowDto r)
            {
                var cells = new List<object?>
                {
                    r.VehicleId, r.ChassisNumber, r.ModelName, r.ColorName
                };
                if (!isSubdealer)
                    cells.Add(r.SubdealerName);
                cells.AddRange(new object?[]
                {
                    r.BookedAging,
                    r.PaperReceivedAging,
                    r.InvoiceAging,
                    r.InsuranceAging,
                    r.AgentAging,
                    r.RegistrationAging,
                    r.SubsidyAging,
                    r.SubsidyDocumentsAging
                });
                return cells;
            }

            return ExcelExportHelper.ToFileResult(
                this,
                $"vehicle_ageing_{DateTime.Now:yyyyMMdd}.xlsx",
                headers,
                rows.Select(Map),
                "Vehicle Ageing");
        }

        private GetVehicleAgingQuery BuildQuery(
            string? dealershipLocation,
            int? subdealerId,
            string? searchTerm,
            bool isSubdealer)
        {
            var query = new GetVehicleAgingQuery
            {
                SearchTerm = searchTerm,
                DealershipLocation = isSubdealer ? null : dealershipLocation
            };
            if (isSubdealer)
                query.SubdealerId = SessionHelper.GetUserId(HttpContext.Session);
            else
            {
                query.DealershipId = SessionHelper.GetDealershipScope(HttpContext.Session);
                query.SubdealerId = subdealerId;
            }
            return query;
        }

        private async Task LoadFiltersAsync(
            string? dealershipLocation,
            int? subdealerId,
            string? searchTerm,
            bool isSubdealer)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsSubdealerView = isSubdealer;
            if (isSubdealer)
                return;

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
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
                allSubdealers = allSubdealers.Where(s => orgRoles.Contains(s.UserId));
            }

            ViewBag.Subdealers = allSubdealers.OrderBy(s => s.GetFullName()).ToList();
            ViewBag.SelectedSubdealerId = subdealerId;
        }

        private static string? Fmt(DateTime? value)
            => value.HasValue ? IstTime.ToIst(value)!.Value.ToString("yyyy-MM-dd") : null;
    }
}

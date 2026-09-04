using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 2, 3, 4)]
    public class GridsController : Controller
    {
        private readonly IMediator _mediator;

        public GridsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> DistinctValues(
            string grid,
            string column,
            string? search,
            int? dealershipId,
            int? subdealerId,
            int? accountId,
            int? id,
            int? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            string? dealershipLocation,
            bool? bookingPhaseOnly)
        {
            if (string.IsNullOrWhiteSpace(grid) || string.IsNullOrWhiteSpace(column))
                return Json(Array.Empty<string>());

            var sessionScope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);

            var values = await _mediator.Send(new GetGridDistinctValuesQuery
            {
                GridId = grid.Trim(),
                Column = column.Trim(),
                Search = search,
                DealershipId = sessionScope ?? dealershipId,
                SubdealerId = subdealerId,
                AccountId = accountId ?? id,
                UserId = SessionHelper.GetUserId(HttpContext.Session),
                Status = status,
                FromDate = from,
                ToDate = to,
                SearchTerm = searchTerm,
                DealershipLocation = dealershipLocation,
                BookingPhaseOnly = bookingPhaseOnly == true,
                Limit = 100
            });

            return Json(values);
        }
    }
}

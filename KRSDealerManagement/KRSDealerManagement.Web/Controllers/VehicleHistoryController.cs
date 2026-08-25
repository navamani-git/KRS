using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Filters;

namespace KRSDealerManagement.Web.Controllers
{
    /// <summary>Admin-only vehicle traceability (chassis lifecycle).</summary>
    public class VehicleHistoryController : Controller
    {
        private readonly IMediator _mediator;

        public VehicleHistoryController(IMediator mediator) => _mediator = mediator;

        [AuthorizeRole(1)]
        public async Task<IActionResult> ChassisHistory(string? chassis)
        {
            chassis = chassis?.Trim().ToUpperInvariant() ?? "";
            ViewBag.ChassisQuery = chassis;

            if (string.IsNullOrWhiteSpace(chassis))
                return View();

            var history = await _mediator.Send(new GetVehicleChassisHistoryQuery { ChassisNumber = chassis });
            if (history == null)
            {
                ViewBag.Error = $"No vehicle found for chassis \"{chassis}\".";
                return View();
            }

            ViewBag.History = history;
            return View();
        }
    }
}

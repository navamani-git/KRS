using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;

namespace KRSDealerManagement.Web.Controllers
{
    /// <summary>Vehicle chassis lifecycle history.</summary>
    public class VehicleHistoryController : Controller
    {
        private readonly IMediator _mediator;

        public VehicleHistoryController(IMediator mediator) => _mediator = mediator;

        [AuthorizeMenu(StaffMenuAccess.ChassisHistory)]
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

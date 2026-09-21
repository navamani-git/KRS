using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Shared.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.PriceUpdateJob, StaffOnly = true)]
    public class PriceUpdateRunsController : Controller
    {
        private readonly IPriceUpdateJobService _jobService;
        private readonly IConfiguration _configuration;

        public PriceUpdateRunsController(IPriceUpdateJobService jobService, IConfiguration configuration)
        {
            _jobService = jobService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var runs = await _jobService.GetRunsAsync();
            ViewBag.SchedulerUrl = BuildSchedulerUrl();
            return View(runs);
        }

        public async Task<IActionResult> Details(int? id, DateTime? date, string? status)
        {
            var filterStatus = string.IsNullOrWhiteSpace(status) ? "Updated" : status;
            var detail = await _jobService.GetRunDetailAsync(id, date, filterStatus);
            if (detail == null)
            {
                TempData["Error"] = date.HasValue
                    ? $"No price update run found for {date.Value:dd-MMM-yyyy}."
                    : "Run not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.FilterDate = detail.AsOfDate.ToString("yyyy-MM-dd");
            ViewBag.FilterStatus = filterStatus;
            return View(detail);
        }

        private string BuildSchedulerUrl()
        {
            var token = _configuration["PriceUpdateJob:SecretToken"];
            if (string.IsNullOrWhiteSpace(token))
                return "(Set PriceUpdateJob:SecretToken in appsettings)";

            return $"{Request.Scheme}://{Request.Host}/Jobs/PriceUpdate?token={Uri.EscapeDataString(token)}";
        }
    }
}

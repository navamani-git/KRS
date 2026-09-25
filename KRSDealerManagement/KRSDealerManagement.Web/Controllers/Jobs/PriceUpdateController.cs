using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Controllers.Jobs
{
    [AllowAnonymous]
    [Route("Jobs")]
    public class PriceUpdateController : Controller
    {
        private readonly IPriceUpdateJobService _jobService;
        private readonly IConfiguration _configuration;

        public PriceUpdateController(IPriceUpdateJobService jobService, IConfiguration configuration)
        {
            _jobService = jobService;
            _configuration = configuration;
        }

        [HttpGet("PriceUpdate")]
        [HttpPost("PriceUpdate")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PriceUpdate(string? token)
        {
            if (!IsAuthorized(token, out var isSchedulerCall, out var userId))
                return Unauthorized(new { success = false, message = "Unauthorized." });

            var trigger = isSchedulerCall ? PriceUpdateTriggerSources.Scheduler : PriceUpdateTriggerSources.Manual;
            var result = await _jobService.RunAsync(null, trigger, userId);

            if (isSchedulerCall)
            {
                return Json(new
                {
                    success = true,
                    runId = result.RunId,
                    status = result.Status,
                    asOfDate = result.AsOfDate.ToString("yyyy-MM-dd"),
                    scanned = result.TotalScanned,
                    updated = result.TotalUpdated,
                    skipped = result.TotalSkipped,
                    errors = result.TotalErrors,
                    summary = result.SummaryMessage
                });
            }

            TempData["Success"] = result.SummaryMessage ?? "Price update completed.";
            return RedirectToAction("Details", "PriceUpdateRuns", new { id = result.RunId });
        }

        private bool IsAuthorized(string? token, out bool isSchedulerCall, out int? userId)
        {
            isSchedulerCall = false;
            userId = null;

            var configToken = _configuration["PriceUpdateJob:SecretToken"];
            if (!string.IsNullOrWhiteSpace(configToken)
                && string.Equals(token, configToken, StringComparison.Ordinal))
            {
                isSchedulerCall = true;
                if (SessionHelper.IsAuthenticated(HttpContext.Session))
                    userId = SessionHelper.GetUserId(HttpContext.Session);
                return true;
            }

            if (!SessionHelper.IsAuthenticated(HttpContext.Session))
                return false;

            if (!SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.PriceUpdateJob))
                return false;

            userId = SessionHelper.GetUserId(HttpContext.Session);
            return true;
        }
    }
}

using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.ViewComponents
{
    public class LocationFilterViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public LocationFilterViewComponent(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!SessionHelper.HasMultipleAssignedLocations(HttpContext.Session))
                return Content(string.Empty);

            var assignedIds = SessionHelper.GetAssignedDealershipIds(HttpContext.Session);
            var dealerships = (await _unitOfWork.Dealerships.GetAllAsync())
                .Where(d => d.IsActive && assignedIds.Contains(d.DealershipId))
                .OrderBy(d => d.DealershipName)
                .Select(d => new LocationFilterOption
                {
                    DealershipId = d.DealershipId,
                    DealershipName = d.DealershipName
                })
                .ToList();

            if (dealerships.Count <= 1)
                return Content(string.Empty);

            return View(new LocationFilterModel
            {
                Options = dealerships,
                SelectedDealershipId = SessionHelper.GetLocationFilterDealershipId(HttpContext.Session),
                ReturnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString
            });
        }
    }

    public class LocationFilterModel
    {
        public List<LocationFilterOption> Options { get; set; } = new();
        public int? SelectedDealershipId { get; set; }
        public string ReturnUrl { get; set; } = "/";
    }

    public class LocationFilterOption
    {
        public int DealershipId { get; set; }
        public string DealershipName { get; set; } = "";
    }
}

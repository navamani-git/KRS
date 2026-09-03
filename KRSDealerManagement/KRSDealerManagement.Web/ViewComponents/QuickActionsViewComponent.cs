using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.ViewComponents
{
    public class QuickActionsViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuickActionsViewComponent(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await QuickActionsHelper.BuildForSessionAsync(
                HttpContext.Session,
                Url,
                _unitOfWork);

            if (model == null || model.Catalog.Count == 0)
                return Content(string.Empty);

            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace KRSDealerManagement.Web.Helpers
{
    public static class GridViewHelper
    {
        public static Dictionary<string, string> SetupGridFilters(Controller controller, string gridId)
        {
            var filters = GridFilterRequestHelper.ReadFilters(controller.Request);
            GridFilterRequestHelper.ApplyToViewBag(controller.ViewBag, filters);
            controller.ViewBag.GridId = gridId;
            controller.ViewBag.GridSortColumn = controller.Request.Query["sort"].ToString();
            controller.ViewBag.GridSortDir = controller.Request.Query["dir"].ToString();
            return filters;
        }
    }
}

using KRSDealerManagement.Web.Helpers.ExcelImport;
using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Helpers.ExcelImport
{
    public static class ExcelImportControllerMixin
    {
        public static IActionResult DownloadImportTemplate(
            this Controller controller,
            ExcelImportService importService,
            string importKey)
        {
            var context = ExcelImportControllerActions.CreateContext(controller.HttpContext, controller.HttpContext.RequestServices);
            return ExcelImportControllerActions.DownloadTemplate(controller, importService, context, importKey);
        }

        public static Task<IActionResult> ImportExcelAsync(
            this Controller controller,
            ExcelImportService importService,
            string importKey,
            IFormFile? file,
            string redirectAction = "Create",
            string? returnController = null)
        {
            var context = ExcelImportControllerActions.CreateContext(controller.HttpContext, controller.HttpContext.RequestServices);
            return ExcelImportControllerActions.ImportAsync(controller, importService, context, importKey, file, redirectAction, returnController);
        }
    }
}

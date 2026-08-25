using KRSDealerManagement.Web.Helpers.ExcelImport;
using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Helpers.ExcelImport
{
    public static class ExcelImportControllerActions
    {
        public static IActionResult DownloadTemplate(Controller controller, ExcelImportService service, ExcelImportContext context, string key)
        {
            var processor = service.GetProcessor(key);
            if (processor == null)
                return controller.NotFound();

            var bytes = service.BuildTemplate(key, context);
            return controller.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                processor.TemplateFileName);
        }

        public static async Task<IActionResult> ImportAsync(
            Controller controller,
            ExcelImportService service,
            ExcelImportContext context,
            string key,
            IFormFile? file,
            string redirectAction,
            string? returnController = null)
        {
            if (file == null || file.Length == 0)
            {
                controller.TempData["Error"] = "Select an Excel file to import.";
                var ctrlEmpty = returnController ?? controller.ControllerContext.ActionDescriptor.ControllerName;
                return controller.RedirectToAction(redirectAction, ctrlEmpty);
            }

            var result = await service.ImportAsync(key, file, context);
            var ctrl = returnController ?? controller.ControllerContext.ActionDescriptor.ControllerName;
            if (result.Success)
            {
                controller.TempData["Success"] =
                    $"Import successful — {result.InsertedCount} record(s) inserted. File saved to {result.SavedRelativePath}.";
                return controller.RedirectToAction("Index", ctrl);
            }

            controller.TempData["Error"] = result.Errors.Count == 1 && result.Errors[0].RowNumber == 0
                ? result.Errors[0].Message
                : $"Import failed — {result.Errors.Count} error(s). No records were inserted.";

            controller.TempData["ImportErrors"] = string.Join("\n", result.Errors.Select(e =>
                e.RowNumber > 0
                    ? $"Row {e.RowNumber}{(string.IsNullOrWhiteSpace(e.Column) ? "" : $" [{e.Column}]")}: {e.Message}"
                    : e.Message));
            if (!string.IsNullOrWhiteSpace(result.SavedRelativePath))
                controller.TempData["ImportSavedPath"] = result.SavedRelativePath;

            return controller.RedirectToAction(redirectAction, ctrl);
        }

        public static ExcelImportContext CreateContext(HttpContext http, IServiceProvider services)
        {
            var userId = SessionHelper.GetUserId(http.Session)
                ?? throw new InvalidOperationException("Not authenticated.");

            return new ExcelImportContext
            {
                UserId = userId,
                DealershipScopeId = SessionHelper.GetDealershipScope(http.Session),
                IsBranchManager = SessionHelper.IsBranchManager(http.Session),
                Services = services
            };
        }
    }
}

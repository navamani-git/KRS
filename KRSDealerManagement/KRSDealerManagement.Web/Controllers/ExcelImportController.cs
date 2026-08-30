using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Helpers.ExcelImport;
using KRSDealerManagement.Web.Services.ExcelImport;

namespace KRSDealerManagement.Web.Controllers
{
    public class ExcelImportController : Controller
    {
        private readonly ExcelImportService _excelImport;

        private static readonly Dictionary<string, (int[] Roles, string? Menu, string RedirectAction)> ImportAuth =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [ExcelImportKeys.RtoLocations] = (new[] { 1 }, StaffMenuAccess.RtoLocations, "Create"),
                [ExcelImportKeys.DocumentTypes] = (new[] { 1 }, StaffMenuAccess.DocumentTypes, "Create"),
                [ExcelImportKeys.FinanceNames] = (new[] { 1 }, StaffMenuAccess.FinanceNames, "Create"),
                [ExcelImportKeys.VehicleColors] = (new[] { 1 }, null, "Create"),
                [ExcelImportKeys.PaymentTypes] = (new[] { 1 }, StaffMenuAccess.PaymentTypes, "Create"),
                [ExcelImportKeys.StatusLookups] = (new[] { 1 }, StaffMenuAccess.StatusLookups, "Create"),
                [ExcelImportKeys.Dealerships] = (new[] { 1 }, StaffMenuAccess.Dealers, "Create"),
                [ExcelImportKeys.VehicleModels] = (new[] { 1 }, null, "Create"),
                [ExcelImportKeys.Prices] = (new[] { 1 }, null, "Create"),
                [ExcelImportKeys.CommissionRates] = (new[] { 1 }, null, "CreateRate"),
                [ExcelImportKeys.StaffUsers] = (new[] { 1 }, StaffMenuAccess.StaffUsers, "Create"),
                [ExcelImportKeys.Subdealers] = (new[] { 1, 4 }, StaffMenuAccess.Subdealers, "Create"),
                [ExcelImportKeys.SubdealerAccounts] = (new[] { 1, 3, 4 }, StaffMenuAccess.Balances, "Create"),
                [ExcelImportKeys.OrdersSubdealer] = (new[] { 2 }, MenuKeys.PurchaseOrderCreate, "Create"),
                [ExcelImportKeys.OrdersForSubdealer] = (new[] { 1, 4 }, null, "CreateForSubdealer"),
                [ExcelImportKeys.VehicleMasters] = (new[] { 1, 4 }, StaffMenuAccess.DealerStock, "Index"),
            };

        public ExcelImportController(ExcelImportService excelImport) => _excelImport = excelImport;

        public IActionResult DownloadTemplate(string key)
        {
            if (!TryAuthorize(key, out var redirect))
                return redirect!;

            return this.DownloadImportTemplate(_excelImport, key);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(string key, IFormFile file, string? returnController, string? returnAction)
        {
            if (!TryAuthorize(key, out var redirect))
                return redirect!;

            if (!ImportAuth.TryGetValue(key, out var meta))
            {
                TempData["Error"] = "Import type is not available.";
                return RedirectToAction("Index", "Dashboard");
            }

            var rc = string.IsNullOrWhiteSpace(returnController) ? RouteKeyToController(key) : returnController;
            var ra = string.IsNullOrWhiteSpace(returnAction) ? meta.RedirectAction : returnAction;

            return await this.ImportExcelAsync(_excelImport, key, file, ra, rc);
        }

        private bool TryAuthorize(string key, out IActionResult? redirect)
        {
            redirect = null;
            if (_excelImport.GetProcessor(key) == null)
            {
                redirect = FileDownloadHelper.RedirectWithMessage(
                    this,
                    "Import template is not available.",
                    RouteKeyToController(key),
                    "Create");
                return false;
            }

            if (!ImportAuth.TryGetValue(key, out var meta))
            {
                redirect = FileDownloadHelper.RedirectWithMessage(
                    this,
                    "Import type is not available.",
                    RouteKeyToController(key),
                    "Create");
                return false;
            }

            var role = SessionHelper.GetUserRole(HttpContext.Session);
            if (!role.HasValue || !meta.Roles.Contains(role.Value))
            {
                redirect = RedirectToAction("AccessDenied", "Account");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(meta.Menu)
                && !SessionHelper.HasMenuAccess(HttpContext.Session, meta.Menu))
            {
                redirect = RedirectToAction("AccessDenied", "Account");
                return false;
            }

            if (key.Equals(ExcelImportKeys.Subdealers, StringComparison.OrdinalIgnoreCase)
                && SessionHelper.IsBranchManager(HttpContext.Session))
            {
                redirect = RedirectToAction("AccessDenied", "Account");
                return false;
            }

            return true;
        }

        private static string RouteKeyToController(string key) => key switch
        {
            ExcelImportKeys.RtoLocations => "RtoLocations",
            ExcelImportKeys.DocumentTypes => "DocumentTypes",
            ExcelImportKeys.FinanceNames => "FinanceNames",
            ExcelImportKeys.VehicleColors => "VehicleColors",
            ExcelImportKeys.PaymentTypes => "PaymentTypes",
            ExcelImportKeys.StatusLookups => "StatusLookups",
            ExcelImportKeys.Dealerships => "Dealerships",
            ExcelImportKeys.VehicleModels => "VehicleModels",
            ExcelImportKeys.Prices => "Prices",
            ExcelImportKeys.CommissionRates => "Commissions",
            ExcelImportKeys.StaffUsers => "StaffUsers",
            ExcelImportKeys.Subdealers => "Subdealers",
            ExcelImportKeys.SubdealerAccounts => "Accounts",
            ExcelImportKeys.OrdersSubdealer => "Orders",
            ExcelImportKeys.OrdersForSubdealer => "Orders",
            ExcelImportKeys.VehicleMasters => "VehicleMasters",
            _ => "Home"
        };
    }
}

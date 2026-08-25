using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.PaymentTypes)]
    public class PaymentTypesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentTypesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index(int? page, int? pageSize)
        {
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.PaymentTypes);
            var list = GridScreenFilterHelper.ApplyPaymentTypes(
                (await _unitOfWork.PaymentTypes.GetAllAsync())
                .OrderByDescending(t => t.IsActive)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.TypeName),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public async Task<IActionResult> Export()
        {
            var list = (await _unitOfWork.PaymentTypes.GetAllAsync())
                .OrderByDescending(t => t.IsActive)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.TypeName)
                .ToList();
            var headers = new[] { "Code", "Name", "Finance Details", "Sort Order", "Status", "Created" };
            var rows = list.Select(t => (IReadOnlyList<object?>)new List<object?>
            {
                t.TypeCode,
                t.TypeName,
                t.RequiresFinanceDetails ? "Yes" : "No",
                t.SortOrder,
                t.IsActive ? "Active" : "Inactive",
                t.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"payment_types_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Payment Types");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string typeCode, string typeName, int sortOrder, bool requiresFinanceDetails)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                TempData["Error"] = "Payment type name is required.";
                return View();
            }

            var code = NormalizeCode(typeCode, typeName);
            var name = typeName.Trim();
            var all = (await _unitOfWork.PaymentTypes.GetAllAsync()).ToList();

            if (all.Any(t => t.TypeCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This type code already exists.";
                return View();
            }

            if (all.Any(t => t.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This payment type name already exists.";
                return View();
            }

            await _unitOfWork.PaymentTypes.AddAsync(new PaymentType
            {
                TypeCode = code,
                TypeName = name,
                RequiresFinanceDetails = requiresFinanceDetails,
                SortOrder = sortOrder > 0 ? sortOrder : all.Count + 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });

            TempData["Success"] = $"Payment type '{name}' added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.PaymentTypes.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, string typeCode, string typeName, int sortOrder, bool requiresFinanceDetails, bool isActive)
        {
            var row = await _unitOfWork.PaymentTypes.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                TempData["Error"] = "Payment type name is required.";
                return View(row);
            }

            var code = NormalizeCode(typeCode, typeName);
            var name = typeName.Trim();
            var all = (await _unitOfWork.PaymentTypes.GetAllAsync()).ToList();

            if (all.Any(t => t.PaymentTypeId != id && t.TypeCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This type code already exists.";
                return View(row);
            }

            if (all.Any(t => t.PaymentTypeId != id && t.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = "This payment type name already exists.";
                return View(row);
            }

            row.TypeCode = code;
            row.TypeName = name;
            row.SortOrder = sortOrder;
            row.RequiresFinanceDetails = requiresFinanceDetails;
            row.IsActive = isActive;
            await _unitOfWork.PaymentTypes.UpdateAsync(row);

            TempData["Success"] = "Payment type updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.PaymentTypes.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction(nameof(Index));
            }

            row.IsActive = false;
            await _unitOfWork.PaymentTypes.UpdateAsync(row);
            TempData["Success"] = "Payment type deactivated.";
            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeCode(string? typeCode, string typeName)
        {
            var raw = string.IsNullOrWhiteSpace(typeCode) ? typeName : typeCode;
            return raw.Trim().ToUpperInvariant().Replace(' ', '_');
        }
    }
}

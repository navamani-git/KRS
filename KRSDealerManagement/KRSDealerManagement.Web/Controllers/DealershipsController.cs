using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)] // System admin only
    [AuthorizeMenu(StaffMenuAccess.Dealers)]
    public class DealershipsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public DealershipsController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int? page)
        {
            var list = await _mediator.Send(new GetDealershipsQuery());
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(list, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            return View(pageItems);
        }

        public async Task<IActionResult> Export()
        {
            var list = (await _mediator.Send(new GetDealershipsQuery())).ToList();
            var headers = new[] { "Code", "Name", "Location", "Phone", "Email", "Subdealers", "Status" };
            var rows = list.Select(d => (IReadOnlyList<object?>)new List<object?>
            {
                d.DealershipCode, d.DealershipName, d.Location ?? "", d.ContactPhone ?? "", d.Email ?? "",
                d.SubDealerCount, d.IsActive ? "Active" : "Inactive"
            });
            return ExcelExportHelper.ToFileResult(this, $"dealerships_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Dealerships");
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string dealershipCode, string dealershipName, string location, string contactPhone, string email)
        {
            if (string.IsNullOrWhiteSpace(dealershipCode) || string.IsNullOrWhiteSpace(dealershipName))
            {
                TempData["Error"] = "Code and Name are required.";
                return View();
            }

            try
            {
                var code = dealershipCode.Trim().ToUpperInvariant().Replace(" ", "_");
                var existing = (await _unitOfWork.Dealerships.GetAllAsync())
                    .Any(d => d.DealershipCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                if (existing)
                {
                    TempData["Error"] = "Dealership code already exists.";
                    return View();
                }

                await _unitOfWork.Dealerships.AddAsync(new Dealership
                {
                    DealershipCode = code,
                    DealershipName = dealershipName.Trim(),
                    Location = location?.Trim(),
                    ContactPhone = contactPhone?.Trim(),
                    Email = email?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                });
                TempData["Success"] = $"Dealership '{dealershipName}' added. Create branch manager & finance users next.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _unitOfWork.Dealerships.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Dealership not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string dealershipName, string location, string contactPhone, string email, bool isActive)
        {
            var row = await _unitOfWork.Dealerships.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Dealership not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(dealershipName))
            {
                TempData["Error"] = "Name is required.";
                return View(row);
            }

            row.DealershipName = dealershipName.Trim();
            row.Location = location?.Trim();
            row.ContactPhone = contactPhone?.Trim();
            row.Email = email?.Trim();
            row.IsActive = isActive;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Dealerships.UpdateAsync(row);
            TempData["Success"] = "Dealership updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _unitOfWork.Dealerships.GetByIdAsync(id);
            if (row == null)
            {
                TempData["Error"] = "Dealership not found.";
                return RedirectToAction(nameof(Index));
            }

            row.IsActive = false;
            row.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Dealerships.UpdateAsync(row);
            TempData["Success"] = $"Dealership '{row.DealershipName}' deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

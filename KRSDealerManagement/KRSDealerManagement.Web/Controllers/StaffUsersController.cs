using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.StaffUsers)]
    public class StaffUsersController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public StaffUsersController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int? staffRole, int? dealershipId, bool? isActive, string? searchTerm, int? page)
        {
            var staff = await _mediator.Send(new GetStaffUsersQuery
            {
                StaffRole = staffRole,
                DealershipId = dealershipId,
                IsActive = isActive,
                SearchTerm = searchTerm
            });

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(staff, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.StaffRole = staffRole;
            ViewBag.DealershipId = dealershipId;
            ViewBag.IsActive = isActive;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery());

            return View(pageItems);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery());
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string fullName, string username, string password,
            int staffRole, int dealershipId,
            string? email, string? phoneNumber)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (staffRole is not ((int)UserRoleEnum.FinanceAdmin) and not ((int)UserRoleEnum.DealerBranchManager))
            {
                TempData["Error"] = "Please select Finance Admin or Branch Manager.";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                var id = await _mediator.Send(new CreateStaffUserCommand
                {
                    FullName = fullName,
                    Username = username,
                    Password = password,
                    StaffRole = staffRole,
                    DealershipId = dealershipId,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Staff user created successfully (login: {username.Trim().ToLowerInvariant()}).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery());
                return View();
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var staff = (await _mediator.Send(new GetStaffUsersQuery())).FirstOrDefault(u => u.UserId == id);
            if (staff == null)
            {
                TempData["Error"] = "Staff user not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery());
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string? email, string? phoneNumber, int dealershipId, bool isActive, string? password)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null || user.UserRole is not ((int)UserRoleEnum.FinanceAdmin) and not ((int)UserRoleEnum.DealerBranchManager))
            {
                TempData["Error"] = "Staff user not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Full name is required.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : user.LastName;
            user.Email = string.IsNullOrWhiteSpace(email) ? user.Email : email.Trim();
            user.PhoneNumber = phoneNumber?.Trim() ?? "";
            user.IsActive = isActive;
            if (!string.IsNullOrWhiteSpace(password))
                user.PasswordHash = password.Trim();
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);

            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync()).Where(a => a.UserId == id).ToList();
            foreach (var a in assignments)
            {
                a.DealershipId = dealershipId;
                a.IsActive = isActive;
                a.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.UserOrgRoles.UpdateAsync(a);
            }

            TempData["Success"] = "Staff user updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Staff user not found.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = false;
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);

            foreach (var a in (await _unitOfWork.UserOrgRoles.GetAllAsync()).Where(x => x.UserId == id))
            {
                a.IsActive = false;
                a.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.UserOrgRoles.UpdateAsync(a);
            }

            TempData["Success"] = $"Staff user '{user.Username}' deactivated.";
            return RedirectToAction(nameof(Index));
        }
    }
}

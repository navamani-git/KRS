using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Models;

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

        public async Task<IActionResult> Index(int? roleId, int? dealershipId, bool? isActive, string? searchTerm, int? page, int? pageSize)
        {
            var staff = await _mediator.Send(new GetStaffUsersQuery
            {
                RoleId = roleId,
                DealershipId = dealershipId,
                IsActive = isActive,
                SearchTerm = searchTerm
            });

            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.StaffUsers);
            staff = GridScreenFilterHelper.ApplyStaffUsers(staff, columnFilters);

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(staff, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.RoleId = roleId;
            ViewBag.DealershipId = dealershipId;
            ViewBag.IsActive = isActive;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            ViewBag.Roles = await _mediator.Send(new GetStaffRolesQuery { AssignableOnly = true, IsActive = true });

            return View(pageItems);
        }

        public async Task<IActionResult> Create()
        {
            await LoadFormViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string fullName, string username, string password,
            int roleId, int dealershipId,
            string? email, string? phoneNumber)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                await _mediator.Send(new CreateStaffUserCommand
                {
                    FullName = fullName,
                    Username = username,
                    Password = password,
                    RoleId = roleId,
                    DealershipId = dealershipId,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    CreatedBy = userId.Value,
                    CanExport = IsFormChecked("canExport"),
                    CanEditWarrantyClaims = IsFormChecked("canEditWarrantyClaims")
                });

                TempData["Success"] = $"Staff user created successfully (login: {username.Trim().ToLowerInvariant()}).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await LoadFormViewBags(dealershipId);
                return View();
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var staff = (await _mediator.Send(new GetStaffUsersQuery())).FirstOrDefault(u => u.UserId == id);
            if (staff == null)
            {
                staff = await BuildStaffUserDtoFromUserAsync(id);
                if (staff == null)
                {
                    TempData["Error"] = "Staff user not found.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await LoadFormViewBags(staff.DealershipId);
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string username, string? email, string? phoneNumber, int dealershipId, int roleId, string? password)
        {
            var isActive = IsFormChecked("isActive");
            var canExport = IsFormChecked("canExport");
            var canEditWarrantyClaims = IsFormChecked("canEditWarrantyClaims");

            var user = await _unitOfWork.Users.GetByIdAsync(id);
            var assignment = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.UserOrgRoleId)
                .FirstOrDefault();
            var role = assignment != null ? await _unitOfWork.Roles.GetByIdAsync(assignment.RoleId) : null;

            if (user == null || role == null || role.IsSystemRole
                || role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Staff user not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Full name is required.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var normalizedUsername = (username ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedUsername) || normalizedUsername.Length < 3)
            {
                TempData["Error"] = "Username must be at least 3 characters.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (normalizedUsername.Any(char.IsWhiteSpace))
            {
                TempData["Error"] = "Username cannot contain spaces.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var allUsers = await _unitOfWork.Users.GetAllAsync();
            if (allUsers.Any(u => u.UserId != id && u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = $"Username '{normalizedUsername}' is already taken.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var selectedRole = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (selectedRole == null || !selectedRole.IsActive || selectedRole.IsSystemRole
                || !selectedRole.DealershipId.HasValue || selectedRole.DealershipId.Value != dealershipId)
            {
                TempData["Error"] = "Selected role does not match the dealership.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var nameParts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            user.Username = normalizedUsername;
            user.Email = string.IsNullOrWhiteSpace(email) ? user.Email : email.Trim();
            user.PhoneNumber = phoneNumber?.Trim() ?? "";
            user.IsActive = isActive;
            user.CanExport = canExport;
            user.CanEditWarrantyClaims = canEditWarrantyClaims;
            user.UserRole = Application.Services.RoleTemplateDefaults.MapTemplateToLegacyUserRole(selectedRole.RoleTemplateCode);
            if (!string.IsNullOrWhiteSpace(password))
                user.PasswordHash = password.Trim();
            user.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);

            foreach (var a in (await _unitOfWork.UserOrgRoles.GetAllAsync()).Where(x => x.UserId == id))
            {
                a.RoleId = roleId;
                a.DealershipId = dealershipId;
                a.IsActive = isActive;
                a.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.UserOrgRoles.UpdateAsync(a);
            }

            await _unitOfWork.SaveChangesAsync();
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

        private async Task LoadFormViewBags(int? dealershipId = null)
        {
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            ViewBag.Roles = await _mediator.Send(new GetStaffRolesQuery { AssignableOnly = true, IsActive = true, DealershipId = dealershipId });
            ViewBag.SelectedDealershipId = dealershipId;
        }

        private bool IsFormChecked(string name)
            => Request.Form[name].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

        private async Task<Application.DTOs.StaffUserDto?> BuildStaffUserDtoFromUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return null;

            var assignment = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.UserOrgRoleId)
                .FirstOrDefault();
            if (assignment == null) return null;

            var role = await _unitOfWork.Roles.GetByIdAsync(assignment.RoleId);
            if (role == null || role.IsSystemRole
                || role.RoleCode.Equals(RoleCodes.SystemAdmin, StringComparison.OrdinalIgnoreCase)
                || role.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var dealership = assignment.DealershipId.HasValue
                ? await _unitOfWork.Dealerships.GetByIdAsync(assignment.DealershipId.Value)
                : null;

            return new Application.DTOs.StaffUserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.GetFullName(),
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserRole = user.UserRole,
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                DealershipId = assignment.DealershipId,
                DealershipName = dealership?.DealershipName,
                IsActive = user.IsActive,
                CanExport = user.CanExport,
                CanEditWarrantyClaims = user.CanEditWarrantyClaims,
                PasswordHash = user.PasswordHash,
                CreatedDate = user.CreatedDate
            };
        }
    }
}

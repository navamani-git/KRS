using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1, 4)]
    [AuthorizeMenu(StaffMenuAccess.Subdealers)]
    public class SubdealersController : Controller
    {
        private readonly IMediator _mediator;

        public SubdealersController(IMediator mediator) => _mediator = mediator;

        public async Task<IActionResult> Index(string searchTerm, bool? isActive, int? page)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive,
                DealershipId = scope
            });
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(subdealers, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsActive = isActive;
            ViewBag.DealershipName = SessionHelper.GetDealershipName(HttpContext.Session);
            return View(pageItems);
        }

        public async Task<IActionResult> Export(string searchTerm, bool? isActive)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = (await _mediator.Send(new GetSubdealersQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive,
                DealershipId = scope
            })).ToList();
            var headers = new[] { "Name", "Email", "Location", "Logins", "Phone", "Status", "Created" };
            var rows = subdealers.Select(s => (IReadOnlyList<object?>)new List<object?>
            {
                s.GetFullName(), s.Email, s.LastName, s.LoginCount,
                s.PhoneNumber, s.IsActive ? "Active" : "Inactive", s.CreatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"subdealers_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Subdealers");
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.CanViewBalances = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);
            await LoadDealershipOptionsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string subdealerName, string email, string location,
            string primaryPhone, string secondaryPhone,
            string salesRepMobile, string serviceRepMobile,
            int dealershipId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            ViewBag.CanViewBalances = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);
            await LoadDealershipOptionsAsync();

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            if (scope.HasValue) dealershipId = scope.Value;

            if (dealershipId <= 0)
            {
                TempData["Error"] = "Select a dealership location.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(subdealerName) || string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(primaryPhone))
            {
                TempData["Error"] = "Name, Location and Primary Phone are required.";
                return View();
            }

            try
            {
                var subDealerId = await _mediator.Send(new CreateSubdealerCommand
                {
                    SubdealerName = subdealerName.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? $"{subdealerName.ToLower().Replace(" ", ".")}@krs.com" : email.Trim(),
                    Location = location.Trim(),
                    PrimaryPhone = primaryPhone.Trim(),
                    SecondaryPhone = string.IsNullOrWhiteSpace(secondaryPhone) ? null : secondaryPhone.Trim(),
                    SalesRepMobile = salesRepMobile?.Trim() ?? "",
                    ServiceRepMobile = serviceRepMobile?.Trim() ?? "",
                    DealershipId = dealershipId,
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Subdealer '{subdealerName}' created. Add at least one login below.";
                return this.RedirectEncrypted(nameof(Details), new { id = subDealerId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealer = await _mediator.Send(new GetSubdealerDetailQuery
            {
                SubDealerId = id,
                DealershipId = scope
            });

            if (subdealer == null)
            {
                subdealer = await _mediator.Send(new GetSubdealerDetailQuery
                {
                    UserId = id,
                    DealershipId = scope
                });
            }

            if (subdealer == null)
            {
                TempData["Error"] = "Subdealer not found (or outside your location).";
                return RedirectToAction(nameof(Index));
            }

            IEnumerable<KRSDealerManagement.Application.DTOs.SubdealerAccountDto> accounts = Array.Empty<KRSDealerManagement.Application.DTOs.SubdealerAccountDto>();
            if (subdealer.PrimaryUserId.HasValue)
            {
                accounts = await _mediator.Send(new GetSubdealerAccountsQuery
                {
                    SubdealerId = subdealer.PrimaryUserId.Value
                });
            }
            ViewBag.Accounts = accounts;

            ViewBag.MenuGroups = MenuKeys.GetSubdealerMenuGroups();
            ViewBag.UseMenuDefaults = true;
            ViewBag.MenuIdPrefix = "newlogin";

            var permMaps = new Dictionary<int, Dictionary<string, bool>>();
            foreach (var login in subdealer.Logins)
            {
                var permissions = await _mediator.Send(new GetAccountPermissionsQuery { AccountId = login.PermissionAccountId });
                permMaps[login.PermissionAccountId] = permissions.ToDictionary(p => p.MenuKey, p => p.IsAccessible, StringComparer.OrdinalIgnoreCase);
            }
            ViewBag.LoginPermMaps = permMaps;
            ViewBag.CanViewBalances = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);
            ViewBag.CanViewStatement = SessionHelper.IsSystemAdmin(HttpContext.Session)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances)
                || SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Subdealers);
            await LoadDealershipOptionsAsync();
            return View(subdealer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int id, string subdealerName, string location, string email,
            string primaryPhone, string? secondaryPhone, string? salesRepMobile, string? serviceRepMobile,
            int dealershipId)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            if (scope.HasValue) dealershipId = scope.Value;

            var isActive = ParseCheckboxValue(Request.Form, "isActive");

            if (string.IsNullOrWhiteSpace(subdealerName) || string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(primaryPhone))
            {
                TempData["Error"] = "Name, location and primary phone are required.";
                return this.RedirectEncrypted(nameof(Details), new { id });
            }

            try
            {
                await _mediator.Send(new UpdateSubdealerOrgCommand
                {
                    SubDealerId = id,
                    SubdealerName = subdealerName.Trim(),
                    Location = location.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? $"{subdealerName.ToLower().Replace(" ", ".")}@krs.com" : email.Trim(),
                    PrimaryPhone = primaryPhone.Trim(),
                    SecondaryPhone = secondaryPhone,
                    SalesRepMobile = salesRepMobile,
                    ServiceRepMobile = serviceRepMobile,
                    IsActive = isActive,
                    DealershipId = dealershipId,
                    UpdatedBy = adminId.Value
                });
                TempData["Success"] = "Subdealer details updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return this.RedirectEncrypted(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLogin(
            int id, string username, string password, string? displayName,
            decimal initialBalance, string[]? accessibleMenus)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            if (!SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances))
                initialBalance = 0;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || password.Trim().Length < 6)
            {
                TempData["Error"] = "Username and password (min 6 chars) are required.";
                return this.RedirectEncrypted(nameof(Details), new { id });
            }

            try
            {
                var menuKeys = accessibleMenus?.Where(m => !string.IsNullOrWhiteSpace(m)).ToList()
                    ?? MenuKeys.GetSubdealerConfigurableMenus().Select(m => m.Key).ToList();

                await _mediator.Send(new CreateSubdealerLoginCommand
                {
                    SubDealerId = id,
                    Username = username.Trim().ToLowerInvariant(),
                    Password = password.Trim(),
                    DisplayName = displayName?.Trim(),
                    InitialBalance = initialBalance,
                    AccessibleMenuKeys = menuKeys,
                    CreatedBy = adminId.Value
                });
                TempData["Success"] = $"Login '{username}' created.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return this.RedirectEncrypted(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(int id, int loginUserId, string password)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return this.RedirectEncrypted(nameof(Details), new { id });
            }

            try
            {
                await _mediator.Send(new SetSubdealerPasswordCommand
                {
                    SubdealerId = loginUserId,
                    Password = password.Trim(),
                    UpdatedBy = adminId.Value
                });
                TempData["Success"] = "Password updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return this.RedirectEncrypted(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUsername(int id, int loginUserId, string username)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(username))
            {
                TempData["Error"] = "Username is required.";
                return this.RedirectEncrypted(nameof(Details), new { id });
            }

            try
            {
                await _mediator.Send(new UpdateSubdealerLoginUsernameCommand
                {
                    SubDealerId = id,
                    LoginUserId = loginUserId,
                    Username = username.Trim().ToLowerInvariant(),
                    UpdatedBy = adminId.Value
                });
                TempData["Success"] = "Username updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return this.RedirectEncrypted(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurePermissions(int id, int accountId, string[]? accessibleMenus)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            var allowed = new HashSet<string>(accessibleMenus ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var settings = MenuKeys.GetSubdealerConfigurableMenus().Select(m =>
            {
                bool on = allowed.Contains(m.Key);
                return new PermissionSetting
                {
                    MenuKey = m.Key,
                    MenuName = m.Name,
                    IsAccessible = on,
                    CanCreate = on,
                    CanEdit = on,
                    CanDelete = false,
                    CanApprove = false
                };
            }).ToList();

            try
            {
                await _mediator.Send(new ConfigureAccountPermissionsCommand
                {
                    AccountId = accountId,
                    Permissions = settings,
                    ConfiguredBy = adminId.Value,
                    Remarks = "Updated from Subdealer Details"
                });
                TempData["Success"] = "Menu permissions saved.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return this.RedirectEncrypted(nameof(Details), new { id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var adminId = SessionHelper.GetUserId(HttpContext.Session);
            if (!adminId.HasValue) return RedirectToAction("Login", "Account");

            var detail = await _mediator.Send(new GetSubdealerDetailQuery { SubDealerId = id });
            if (detail == null)
            {
                TempData["Error"] = "Subdealer not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new UpdateSubdealerOrgCommand
                {
                    SubDealerId = id,
                    SubdealerName = detail.SubdealerName,
                    Location = detail.Location,
                    Email = detail.Email,
                    PrimaryPhone = detail.PrimaryPhone,
                    SecondaryPhone = detail.SecondaryPhone,
                    SalesRepMobile = detail.SalesRepMobile,
                    ServiceRepMobile = detail.ServiceRepMobile,
                    IsActive = false,
                    DealershipId = detail.DealershipId,
                    UpdatedBy = adminId.Value
                });
                TempData["Success"] = "Subdealer deactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDealershipOptionsAsync()
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var all = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            if (scope.HasValue)
                all = all.Where(d => d.DealershipId == scope.Value);
            ViewBag.Dealerships = all;
            ViewBag.LockedDealershipId = scope;
        }

        private static bool ParseCheckboxValue(IFormCollection form, string key)
        {
            if (!form.TryGetValue(key, out var values)) return false;
            return values.Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
        }
    }
}

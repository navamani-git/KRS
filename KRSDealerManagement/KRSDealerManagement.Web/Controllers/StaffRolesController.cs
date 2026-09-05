using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.StaffRoles)]
    public class StaffRolesController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IRoleTemplateService _roleTemplateService;

        public StaffRolesController(IMediator mediator, IRoleTemplateService roleTemplateService)
        {
            _mediator = mediator;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<IActionResult> Index(int? dealershipId, bool? isActive, string? searchTerm)
        {
            var roles = await _mediator.Send(new GetStaffRolesQuery
            {
                DealershipId = dealershipId,
                IsActive = isActive,
                SearchTerm = searchTerm
            });

            ViewBag.DealershipId = dealershipId;
            ViewBag.IsActive = isActive;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            return View(roles);
        }

        public async Task<IActionResult> Create()
        {
            await LoadFormViewBags();
            return View(new StaffRoleFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffRoleFormModel model)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                var id = await _mediator.Send(new CreateStaffRoleCommand
                {
                    RoleCode = model.RoleCode,
                    RoleName = model.RoleName,
                    Description = model.Description,
                    RoleTemplateCode = string.IsNullOrWhiteSpace(model.RoleTemplateCode)
                        ? RoleTemplateCodes.Custom
                        : model.RoleTemplateCode,
                    DealershipId = model.DealershipId,
                    Menus = ParseMenus(model),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Role '{model.RoleName}' created.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await LoadFormViewBags(model.DealershipId);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _mediator.Send(new GetStaffRoleByIdQuery { RoleId = id });
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadFormViewBags(role.DealershipId);
            return View(MapToForm(role));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StaffRoleFormModel model)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                await _mediator.Send(new UpdateStaffRoleCommand
                {
                    RoleId = id,
                    RoleName = model.RoleName,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    Menus = ParseMenus(model),
                    ModifiedBy = userId.Value
                });

                TempData["Success"] = "Role updated.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                model.RoleId = id;
                await LoadFormViewBags(model.DealershipId);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult SuggestCode(int dealershipId)
        {
            var dealerships = _mediator.Send(new GetDealershipsQuery { IsActive = true }).GetAwaiter().GetResult();
            var dealer = dealerships.FirstOrDefault(d => d.DealershipId == dealershipId);
            if (dealer == null) return Json(new { code = "" });
            var code = _roleTemplateService.BuildSuggestedRoleCode(dealer.DealershipCode, RoleTemplateCodes.Custom);
            return Json(new { code });
        }

        private async Task LoadFormViewBags(int? dealershipId = null)
        {
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            ViewBag.AllMenus = StaffMenuAccess.AllAdminMenus();
            ViewBag.DefaultMenus = new Dictionary<string, int>();
            ViewBag.SelectedDealershipId = dealershipId;
        }

        private static List<RoleMenuPermissionInput> ParseMenus(StaffRoleFormModel model)
        {
            return model.MenuKeys
                .Zip(model.MenuAccessLevels, (key, level) => new RoleMenuPermissionInput
                {
                    MenuKey = key,
                    AccessLevel = (MenuAccessLevel)level
                })
                .GroupBy(m => m.MenuKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(m => (int)m.AccessLevel).First())
                .ToList();
        }

        private static StaffRoleFormModel MapToForm(Application.DTOs.StaffRoleDto role)
        {
            var allMenuKeys = StaffMenuAccess.AllAdminMenus().Select(m => m.Key).ToList();
            var enabledLevels = role.Menus.ToDictionary(
                m => m.MenuKey,
                m => (int)m.AccessLevel,
                StringComparer.OrdinalIgnoreCase);

            return new StaffRoleFormModel
            {
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                RoleTemplateCode = role.RoleTemplateCode ?? RoleTemplateCodes.Custom,
                DealershipId = role.DealershipId ?? 0,
                IsActive = role.IsActive,
                MenuKeys = allMenuKeys,
                MenuAccessLevels = allMenuKeys
                    .Select(key => enabledLevels.TryGetValue(key, out var level) ? level : (int)MenuAccessLevel.None)
                    .ToList()
            };
        }
    }

    public class StaffRoleFormModel
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public string RoleTemplateCode { get; set; } = RoleTemplateCodes.Custom;
        public int DealershipId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> MenuKeys { get; set; } = new();
        public List<int> MenuAccessLevels { get; set; } = new();
    }
}

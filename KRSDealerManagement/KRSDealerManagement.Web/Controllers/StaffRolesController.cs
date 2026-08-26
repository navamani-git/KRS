using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.StaffRoles)]
    public class StaffRolesController : Controller
    {
        private readonly IMediator _mediator;

        public StaffRolesController(IMediator mediator) => _mediator = mediator;

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
                    RoleTemplateCode = model.RoleTemplateCode,
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
                await LoadFormViewBags(model.DealershipId, model.RoleTemplateCode);
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

            await LoadFormViewBags(role.DealershipId, role.RoleTemplateCode);
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
                await LoadFormViewBags(model.DealershipId, model.RoleTemplateCode);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult TemplateDefaults(string templateCode)
        {
            var defaults = Application.Services.RoleTemplateDefaults.GetDefaultMenus(templateCode)
                .ToDictionary(kv => kv.Key, kv => (int)kv.Value);
            return Json(defaults);
        }

        [HttpGet]
        public IActionResult SuggestCode(int dealershipId, string templateCode)
        {
            var dealerships = _mediator.Send(new GetDealershipsQuery { IsActive = true }).GetAwaiter().GetResult();
            var dealer = dealerships.FirstOrDefault(d => d.DealershipId == dealershipId);
            if (dealer == null) return Json(new { code = "" });
            var code = Application.Services.RoleTemplateDefaults.BuildSuggestedRoleCode(dealer.DealershipCode, templateCode);
            return Json(new { code });
        }

        private async Task LoadFormViewBags(int? dealershipId = null, string? templateCode = null)
        {
            ViewBag.Dealerships = await _mediator.Send(new GetDealershipsQuery { IsActive = true });
            ViewBag.Templates = RoleTemplateCodes.All;
            ViewBag.AllMenus = StaffMenuAccess.AllAdminMenus();
            ViewBag.DefaultMenus = Application.Services.RoleTemplateDefaults.GetDefaultMenus(templateCode ?? RoleTemplateCodes.Manager)
                .ToDictionary(kv => kv.Key, kv => (int)kv.Value);
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
                .ToList();
        }

        private static StaffRoleFormModel MapToForm(Application.DTOs.StaffRoleDto role) => new()
        {
            RoleId = role.RoleId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            Description = role.Description,
            RoleTemplateCode = role.RoleTemplateCode ?? RoleTemplateCodes.Custom,
            DealershipId = role.DealershipId ?? 0,
            IsActive = role.IsActive,
            MenuKeys = role.Menus.Select(m => m.MenuKey).ToList(),
            MenuAccessLevels = role.Menus.Select(m => (int)m.AccessLevel).ToList()
        };
    }

    public class StaffRoleFormModel
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string? Description { get; set; }
        public string RoleTemplateCode { get; set; } = RoleTemplateCodes.Manager;
        public int DealershipId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> MenuKeys { get; set; } = new();
        public List<int> MenuAccessLevels { get; set; } = new();
    }
}

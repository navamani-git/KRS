using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Enums;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    [AuthorizeMenu(StaffMenuAccess.RoleTemplates)]
    public class RoleTemplatesController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IRoleTemplateService _roleTemplateService;

        public RoleTemplatesController(IMediator mediator, IRoleTemplateService roleTemplateService)
        {
            _mediator = mediator;
            _roleTemplateService = roleTemplateService;
        }

        public async Task<IActionResult> Index()
        {
            var templates = await _mediator.Send(new GetRoleTemplatesQuery { IncludeInactive = true });
            return View(templates);
        }

        public async Task<IActionResult> Create()
        {
            await LoadFormViewBags();
            return View(new RoleTemplateFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleTemplateFormModel model)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                var id = await _mediator.Send(new CreateRoleTemplateCommand
                {
                    TemplateCode = model.TemplateCode,
                    TemplateName = model.TemplateName,
                    Description = model.Description,
                    LegacyUserRole = model.LegacyUserRole,
                    Menus = ParseMenus(model),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Role template '{model.TemplateName}' created.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await LoadFormViewBags();
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var template = await _mediator.Send(new GetRoleTemplateByIdQuery { RoleTemplateId = id });
            if (template == null)
            {
                TempData["Error"] = "Role template not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadFormViewBags(template.Menus);
            return View(MapToForm(template));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoleTemplateFormModel model)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                await _mediator.Send(new UpdateRoleTemplateCommand
                {
                    RoleTemplateId = id,
                    TemplateName = model.TemplateName,
                    Description = model.Description,
                    LegacyUserRole = model.LegacyUserRole,
                    IsActive = model.IsActive,
                    Menus = ParseMenus(model),
                    ModifiedBy = userId.Value
                });

                TempData["Success"] = "Role template updated.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                model.RoleTemplateId = id;
                await LoadFormViewBags(ParseMenus(model));
                return View(model);
            }
        }

        private async Task LoadFormViewBags(List<RoleMenuPermissionInput>? selectedMenus = null)
        {
            ViewBag.AllMenus = StaffMenuAccess.AllAdminMenus();
            ViewBag.DefaultMenus = selectedMenus?.ToDictionary(m => m.MenuKey, m => (int)m.AccessLevel)
                ?? new Dictionary<string, int>();
            ViewBag.RoleForm = new StaffRoleFormModel
            {
                MenuKeys = selectedMenus?.Select(m => m.MenuKey).ToList() ?? new List<string>(),
                MenuAccessLevels = selectedMenus?.Select(m => (int)m.AccessLevel).ToList() ?? new List<int>()
            };
            await Task.CompletedTask;
        }

        private static List<RoleMenuPermissionInput> ParseMenus(RoleTemplateFormModel model)
            => model.MenuKeys
                .Zip(model.MenuAccessLevels, (key, level) => new RoleMenuPermissionInput
                {
                    MenuKey = key,
                    AccessLevel = (MenuAccessLevel)level
                })
                .ToList();

        private static RoleTemplateFormModel MapToForm(RoleTemplateDetailDto template) => new()
        {
            RoleTemplateId = template.RoleTemplateId,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            Description = template.Description,
            LegacyUserRole = template.LegacyUserRole,
            IsActive = template.IsActive,
            MenuKeys = template.Menus.Select(m => m.MenuKey).ToList(),
            MenuAccessLevels = template.Menus.Select(m => (int)m.AccessLevel).ToList()
        };
    }

    public class RoleTemplateFormModel
    {
        public int RoleTemplateId { get; set; }
        public string TemplateCode { get; set; } = "";
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public int LegacyUserRole { get; set; } = 4;
        public bool IsActive { get; set; } = true;
        public List<string> MenuKeys { get; set; } = new();
        public List<int> MenuAccessLevels { get; set; } = new();
    }
}

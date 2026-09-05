using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Web.Filters;

namespace KRSDealerManagement.Web.Controllers
{
    [AuthorizeRole(1)]
    public class RoleTemplatesController : Controller
    {
        public IActionResult Index() => RedirectToStaffRoles();
        public IActionResult Create() => RedirectToStaffRoles();
        public IActionResult Edit(int id) => RedirectToStaffRoles();
        public IActionResult Configure(string code) => RedirectToStaffRoles();

        private IActionResult RedirectToStaffRoles()
        {
            TempData["Success"] = "Role Templates is no longer used. Create and edit access on Staff Roles.";
            return RedirectToAction("Index", "StaffRoles");
        }
    }
}

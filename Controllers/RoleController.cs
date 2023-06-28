using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private RoleManager<IdentityRole> _roleManager;
        public RoleController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public ViewResult Index()
        {
            if (TempData["ErrorMessage"] != null)
            {
                ModelState.AddModelError("CustomError", TempData["ErrorMessage"].ToString());
            }

            return View();
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Required] string RoleName)
        {
            if (ModelState.IsValid)
            {
                var role = await _roleManager.RoleExistsAsync(RoleName);
                if (!role)
                {
                    IdentityResult identityResult = await _roleManager.CreateAsync(new IdentityRole(RoleName));
                    if (identityResult.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create role '" + RoleName + "'";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Role '" + RoleName + "' exists!";
                }
            }
            return RedirectToAction("Index");
        }


    }
}
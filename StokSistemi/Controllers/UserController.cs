using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StokSistemi.Models;
using StokSistemi.Services;

namespace YAZLAB2.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<Customer> _userManager; // UserManager tanımı

        private readonly SignInManager<Customer> _signInManager;


        public UserController(UserManager<Customer> userManager, AdminService adminService, SignInManager<Customer> signInManager)
        {
            _userManager = userManager; // UserManager'ı atama
            _signInManager = signInManager;

        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<JsonResult> AdminRegister()
        {
            var Appuser = new Customer
            {
                Id = "admin",
                UserName = "admin",
                Budget = 0,
                CustomerType = "admin",
                TotalSpent = 0,
                PriorityScore = 0,
                WaitingTime = TimeSpan.Zero,

            };

            IdentityResult result = await _userManager.CreateAsync(Appuser, "AWDj#BBGAq2q2C");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(Appuser, "Admin");
                return Json("Kayıt Başarılı");
            }

            return Json("Kayıt Başarısız");
        }
        [HttpPost]
        public async Task<IActionResult> Login(UserLoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Şifre, false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Username);

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    TempData["SuccessMessage"] = "Hoşgeldin Admin";
                    return Redirect("/Admin/Index");
                }
                else if (await _userManager.IsInRoleAsync(user, "User"))
                {
                    TempData["SuccessMessage"] = "Hoşgeldin";
                    return RedirectToAction("CustomerArea", "Customer", new { Username = model.Username });
                }

                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            }

            return View(model);
        }
    }
}

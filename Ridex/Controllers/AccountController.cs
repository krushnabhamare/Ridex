using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Ridex.Models;
using Ridex.Services;
using Ridex.ViewModels;

namespace Ridex.Controllers
{
    public class AccountController : Controller
    {
        private const string AdminSecretKey = "RIDEX@ADMIN2026";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // =============================
        // Register
        // =============================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Model validation
            if (!ModelState.IsValid)
                return View(model);

            // Duplicate email check
            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("","Email already registered.");

                return View(model);
            }

            // Admin secret validation
            if (model.RoleName == "Admin" &&
                model.AdminKey != AdminSecretKey)
            {
                ModelState.AddModelError("","Invalid Admin Key.");

                return View(model);
            }

            // Create role if not exists
            if (!await _roleManager.RoleExistsAsync(model.RoleName))
            {
                await _roleManager.CreateAsync(
                    new IdentityRole(model.RoleName));
            }

            // Create user
            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                RoleName = model.RoleName,
                EmailConfirmed = true
            };

            // Password creation
            var result = await _userManager.CreateAsync(user,model.Password);

            // Identity errors
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("",error.Description);
                }

                return View(model);
            }

            // Assign role
            await _userManager.AddToRoleAsync(user,model.RoleName);

            // Auto login
            await _signInManager.SignInAsync(user,isPersistent: false);

            // Redirect by role
            return model.RoleName switch
            {
                "Admin" =>
                    RedirectToAction(
                        "Dashboard",
                        "Admin"),

                "Driver" =>
                    RedirectToAction(
                        "Dashboard",
                        "Driver"),

                _ =>
                    RedirectToAction(
                        "Dashboard",
                        "Rider")
            };
        }

        // =============================
        // Login
        // =============================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Model validation
            if (!ModelState.IsValid)
                return View(model);

            // Find user by email
            var user =
                await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("","Invalid email or password.");

                return View(model);
            }

            // Login attempt
            var result =
                await _signInManager.PasswordSignInAsync(user.UserName,model.Password,false,lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("","Invalid email or password.");

                return View(model);
            }

            // Role based redirect
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard","Admin");
            }

            if (await _userManager.IsInRoleAsync(user, "Driver"))
            {
                return RedirectToAction("Dashboard","Driver");
            }

            return RedirectToAction("Dashboard","Rider");
        }

        // =============================
        // Logout
        // =============================

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login");
        }

        // =============================
        // Forgot Password
        // =============================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            // security safe response
            if (user == null)
            {
                TempData["Success"] =
                    "If email exists, password reset link has been sent.";

                return RedirectToAction("Login");
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action("ResetPassword","Account",
                new
                {
                    token,
                    email = user.Email
                },
                Request.Scheme);

            await _emailService.SendOtpEmailAsync(user.Email,user.FullName, $"Reset your password using this link:\n{resetLink}");

            TempData["Success"] =
                "Password reset link sent to your email.";

            return RedirectToAction("Login");
        }

        // =============================
        // Reset Password
        // =============================

        [HttpGet]
        public IActionResult ResetPassword(
            string token,
            string email)
        {
            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user =
                await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid request.");

                return View(model);
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }

            TempData["Success"] =
                "Password changed successfully.";

            return RedirectToAction("Login");
        }
    }
}
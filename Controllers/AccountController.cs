using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Extensions;
using panelapp.Models;
using panelapp.ViewModels;

namespace panelapp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.GetCurrentUserId().HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Active);

            if (user == null)
            {
                model.ErrorMessage = "Λάθος όνομα χρήστη η κωδικός.";
                return View(model);
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                model.ErrorMessage = "Λάθος όνομα χρήστη η κωδικός.";
                return View(model);
            }

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty);
            HttpContext.Session.SetString("RoleName", user.RoleName ?? string.Empty);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RaceDayMVC.Helpers;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Text.Json;
namespace RaceDayMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiClient _api;
        private static readonly JsonSerializerOptions Opts = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public AccountController(ApiClient api) => _api = api;
        // ---------- LOGIN ---------
        [HttpGet]
        public IActionResult Login()
        {
            if (AuthCheck.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Index", AuthCheck.IsOrganiser(HttpContext.Session) ?
                  "Organiser" : "Participant");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Email and password are required.";
                return View(model);
            }
            var r = await _api.PostAsync("api/auth/login",
                new { email = model.Email, passwordHash = model.Password });
            if (!r.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View(model);
            }
            var json = await r.Content.ReadAsStringAsync();
            
            var auth = JsonSerializer.Deserialize<AuthResponseVm>(json, Opts)!;
            // Store everything from the login response — no second /me call needed
            HttpContext.Session.SetString("JwtToken", auth.Token);
            HttpContext.Session.SetInt32("UserId", auth.User.UserID);
            HttpContext.Session.SetString("FullName", auth.User.FullName);
            HttpContext.Session.SetString("Email", auth.User.Email);
            HttpContext.Session.SetString("Role", auth.User.Role);  // "Organiser" or "Participant"
            return auth.User.Role == "Organiser"
                ? RedirectToAction("Index", "Organiser")
                : RedirectToAction("Index", "Participant");
        }
        // ---------- REGISTER ---------
        [HttpGet]
        public IActionResult Register() => View();
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Email and password are required.";
                return View(model);
            }
            // Your API's Register accepts a User object directly
            var r = await _api.PostAsync("api/auth/register", new
            {
                fullName = model.FullName,
                email = model.Email,
                passwordHash = model.Password,      // ← field is PasswordHash in your model
                role = model.Role
            });
            if (!r.IsSuccessStatusCode)
            {
                ViewBag.Error = await r.Content.ReadAsStringAsync();
                return View(model);
            }
            TempData["Success"] = "Account created. Please log in.";
            return RedirectToAction("Login");
        }
        // ---------- LOGOUT ---------
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        // ---------- ERROR ---------
        public IActionResult Error() => View();
    }
}

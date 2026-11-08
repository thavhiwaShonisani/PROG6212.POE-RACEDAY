using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RaceDayMVC.Helpers;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Text.Json;
namespace RaceDayMVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiClient _api;
        private static readonly JsonSerializerOptions Opts = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public ParticipantController(ApiClient api) => _api = api;
        private bool Guard() => AuthCheck.IsParticipant(HttpContext.Session);
        // ---------- BROWSE EVENTS ---------
        public async Task<IActionResult> Index()
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.GetAsync("api/events");
            var json = await r.Content.ReadAsStringAsync();
            var events = JsonSerializer.Deserialize<List<EventVm>>(json, Opts) ?? new();
            return View(events);
        }
        // ---------- EVENT DETAILS ---------
        public async Task<IActionResult> Details(int id)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var evR = await _api.GetAsync($"api/events/{id}");
            if (!evR.IsSuccessStatusCode) return NotFound();
            var ev = JsonSerializer.Deserialize<EventVm>(
                await evR.Content.ReadAsStringAsync(), Opts)!;
            // Categories via separate endpoint (they're [JsonIgnore] on Event)
            var catR = await _api.GetAsync($"api/events/{id}/categories");
            var cats = catR.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<CategoryVm>>(
                    await catR.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            
            // My enrolments — to know which categories I've joined
            var myR = await _api.GetAsync("api/enrolments");
            var myEnrolments = myR.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<EnrolmentVm>>(
                    await myR.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            ViewBag.Categories = cats;
            ViewBag.MyCategoryIds = myEnrolments.Select(e => e.CategoryID).ToHashSet();
            return View(ev);
        }
        // ---------- ENROL ---------
        [HttpPost]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            // ParticipantID is server-forced by API — never sent from client
            var r = await _api.PostAsync("api/enrolments", new { categoryID = categoryId });
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] = r.IsSuccessStatusCode
                ? "Successfully enrolled!"
                : await r.Content.ReadAsStringAsync();
            return RedirectToAction("Details", new { id = eventId });
        }
        // ---------- MY ENROLMENTS ---------
        public async Task<IActionResult> MyEnrolments()
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            // GET /api/enrolments auto-scopes to the current participant
            var r = await _api.GetAsync("api/enrolments");
            var list = JsonSerializer.Deserialize<List<EnrolmentVm>>(
                await r.Content.ReadAsStringAsync(), Opts) ?? new();
            return View(list);
        }
        // ---------- CANCEL ENROLMENT ---------
        [HttpPost]
        public async Task<IActionResult> CancelEnrolment(int id)

        {

            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.DeleteAsync($"api/enrolments/{id}");
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] =
                    r.IsSuccessStatusCode ? "Enrolment cancelled." : "Could not cancel.";
            return RedirectToAction("MyEnrolments");
        }
        // ---------- MY RESULTS ---------
        public async Task<IActionResult> MyResults()
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            // GET /api/results auto-scopes to the current participant
            var r = await _api.GetAsync("api/results");
            var list = JsonSerializer.Deserialize<List<ResultVm>>(
                await r.Content.ReadAsStringAsync(), Opts) ?? new();
            return View(list);
        }
        // ---------- PROFILE ---------
        public async Task<IActionResult> Profile()
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.GetAsync("api/auth/me");
            var me = JsonSerializer.Deserialize<MeResponseVm>(
                await r.Content.ReadAsStringAsync(), Opts)!;
            return View(me);
        }
        [HttpPost]
        public async Task<IActionResult> Profile(MeResponseVm model)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            // PUT /api/auth/me accepts a User object; passwordHash empty = keep current
            var r = await _api.PutAsync("api/auth/me", new
            {
                fullName = model.FullName,
                email = model.Email,
                passwordHash = ""       // empty = don't change password
            });
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] =
                r.IsSuccessStatusCode ? "Profile updated." : "Update failed.";
            // Refresh session full name in case it changed
            if (r.IsSuccessStatusCode)
                HttpContext.Session.SetString("FullName", model.FullName);
            return RedirectToAction("Profile");
        }
    }
}
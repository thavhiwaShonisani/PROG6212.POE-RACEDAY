using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Helpers;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Text.Json;
namespace RaceDayMVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly ApiClient _api;
        private static readonly JsonSerializerOptions Opts = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public OrganiserController(ApiClient api) => _api = api;
        private bool Guard() => AuthCheck.IsOrganiser(HttpContext.Session);
        private int Me => AuthCheck.CurrentUserId(HttpContext.Session);
        // ---------- DASHBOARD ---------
        public async Task<IActionResult> Index()
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            // GET /api/events returns ALL events — filter client-side by organiser
            var evR = await _api.GetAsync("api/events");
            var all = JsonSerializer.Deserialize<List<EventVm>>(
                await evR.Content.ReadAsStringAsync(), Opts) ?? new();
            var mine = all.Where(e => e.OrganiserID == Me).ToList();
            // GET /api/enrolments is already scoped to my events
            var enR = await _api.GetAsync("api/enrolments");
            var enrolments = enR.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<EnrolmentVm>>(
                    await enR.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            // Count enrolments per event (via enrolment.Category.EventID)
            var counts = enrolments
                .Where(e => e.Category != null)
                .GroupBy(e => e.Category!.EventID)
                .ToDictionary(g => g.Key, g => g.Count());
            return View(new DashboardVm { Events = mine, EnrolmentCounts = counts });
        }
        // ---------- CREATE EVENT ---------
        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventVm model)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.PostAsync("api/events", new
            {
                eventName = model.EventName,
                description = model.Description ?? "",
                eventDate = model.EventDate,
                location = model.Location,
                distanceKm = model.DistanceKm,
                eventType = model.EventType   // "Run" / "Walk" / "Cycle"
            });
            if (!r.IsSuccessStatusCode)
            {
                ViewBag.Error = await r.Content.ReadAsStringAsync();
                return View(model);
            }
            var created = JsonSerializer.Deserialize<EventVm>(
                await r.Content.ReadAsStringAsync(), Opts)!;
            if (model.BannerFile != null && model.BannerFile.Length > 0)
            {
                var br = await _api.PostMultipartAsync(
                    $"api/events/{created.EventID}/banner", model.BannerFile, "file");
                if (!br.IsSuccessStatusCode)
                    TempData["Error"] = "Event created, but banner upload failed.";
            }
            TempData["Success"] = "Event created!";
            return RedirectToAction("Index");
        }
        // ---------- EDIT EVENT ---------
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.GetAsync($"api/events/{id}");
            if (!r.IsSuccessStatusCode) return NotFound();
            var ev = JsonSerializer.Deserialize<EventVm>(
                await r.Content.ReadAsStringAsync(), Opts)!;
            if (ev.OrganiserID != Me) return Forbid();
            return View(ev);
        }
        [HttpPost]
        public async Task<IActionResult> EditEvent(int id, EventVm model, IFormFile? bannerFile)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.PutAsync($"api/events/{id}", new
            {
                eventID = id,
                organiserID = Me,
                eventName = model.EventName,
                description = model.Description ?? "",
                eventDate = model.EventDate,
                location = model.Location,
                distanceKm = model.DistanceKm,
                eventType = model.EventType,
                bannerImageUrl = model.BannerImageUrl,
                createdAt = model.CreatedAt
            });
            if (!r.IsSuccessStatusCode)
            {
                ViewBag.Error = await r.Content.ReadAsStringAsync();
                return View(model);
            }
            if (bannerFile != null && bannerFile.Length > 0)
            {
                var br = await _api.PostMultipartAsync(
                    $"api/events/{id}/banner", bannerFile, "file");
                if (!br.IsSuccessStatusCode)
                    TempData["Error"] = "Event updated, but banner upload failed.";
            }
            TempData["Success"] = "Event updated.";
            return RedirectToAction("Index");
        }
        // ---------- DELETE EVENT ---------
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.DeleteAsync($"api/events/{id}");
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] =
                r.IsSuccessStatusCode ? "Event deleted." : "Delete failed.";
            return RedirectToAction("Index");
        }
        // ---------- CATEGORIES FOR AN EVENT ---------
        public async Task<IActionResult> Categories(int eventId)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.GetAsync($"api/events/{eventId}/categories");
            var cats = r.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<CategoryVm>>(
                    await r.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            var evR = await _api.GetAsync($"api/events/{eventId}");
            var ev = evR.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<EventVm>(
                    await evR.Content.ReadAsStringAsync(), Opts)
                : null;
            ViewBag.EventId = eventId;
            ViewBag.EventName = ev?.EventName ?? "";
            return View(cats);
        }
        [HttpPost]
        public async Task<IActionResult> AddCategory(int eventId, string categoryName, decimal
          distanceKm, decimal entryFee)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Category name is required.";
                return RedirectToAction("Categories", new { eventId });
            }
            var r = await _api.PostAsync("api/categories", new
            {
                eventID = eventId,
                categoryName = categoryName.Trim(),
                distanceKm,
                entryFee
            });
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] =
                r.IsSuccessStatusCode ? "Category added." : await r.Content.ReadAsStringAsync();
            return RedirectToAction("Categories", new { eventId });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int eventId, int id)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var r = await _api.DeleteAsync($"api/categories/{id}");
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] =
                r.IsSuccessStatusCode ? "Category deleted." : await r.Content.ReadAsStringAsync();
            return RedirectToAction("Categories", new { eventId });
        }
        // ---------- ENROLMENTS FOR AN EVENT ---------
        public async Task<IActionResult> Enrolments(int eventId)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            // Fetch all my enrolments, filter client-side by eventId
            var r = await _api.GetAsync("api/enrolments");
            var all = r.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<EnrolmentVm>>(
                    await r.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            var forEvent = all.Where(e => e.Category?.EventID == eventId).ToList();
            ViewBag.EventId = eventId;
            return View(forEvent);
        }
        // ---------- CAPTURE RESULTS ---------
        public async Task<IActionResult> CaptureResults(int eventId)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            var enR = await _api.GetAsync("api/enrolments");
            var enrolments = enR.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<EnrolmentVm>>(
                    await enR.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            enrolments = enrolments.Where(e => e.Category?.EventID == eventId).ToList();
            var resR = await _api.GetAsync("api/results");
            var results = resR.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<List<ResultVm>>(
                    await resR.Content.ReadAsStringAsync(), Opts) ?? new()
                : new();
            // results already scoped to my events via API
            ViewBag.EventId = eventId;
            ViewBag.Results = results.Where(r => r.Enrolment?.Category?.EventID == eventId).ToList();
            return View(enrolments);
        }
        [HttpPost]
        public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId,
            string finishTime, int? position, string status, int? existingResultId)
        {
            if (!Guard()) return RedirectToAction("Login", "Account");
            TimeSpan? span = null;
            if (!string.IsNullOrWhiteSpace(finishTime))
            {
                if (!TimeSpan.TryParse(finishTime, out var parsed))
                {
                    TempData["Error"] = "Finish time must be HH:MM:SS.";
                    return RedirectToAction("CaptureResults", new { eventId });
                }
                span = parsed;
            }
            // Build payload — JsonSerializer handles TimeSpan → ISO 8601 automatically
            var payload = new
            {
                enrolmentID = enrolmentId,
                finishTime = span,
                position = position,
                status = string.IsNullOrWhiteSpace(status) ? "Finished" : status
            };
            HttpResponseMessage r;
            if (existingResultId.HasValue && existingResultId.Value > 0)
            {
                // Update existing
                r = await _api.PutAsync($"api/results/{existingResultId.Value}", new
                {
                    resultID = existingResultId.Value,
                    enrolmentID = enrolmentId,
                    finishTime = span,
                    position = position,
                    status = payload.status
                });
            }
            else
            {
                // Create new
                r = await _api.PostAsync("api/results", payload);
            }
            TempData[r.IsSuccessStatusCode ? "Success" : "Error"] =
                r.IsSuccessStatusCode ? "Result saved." : await r.Content.ReadAsStringAsync();
            return RedirectToAction("CaptureResults", new { eventId });
        }
    }
}

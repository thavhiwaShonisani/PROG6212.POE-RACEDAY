using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace RaceDayManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private static readonly string[] ValidTypes = { "Run", "Walk", "Cycle" };

        private readonly RaceDayDbContext _db;
        public EventsController(RaceDayDbContext db) => _db = db;

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _db.Events
                            .Include(e => e.Organiser)
                            .AsNoTracking()
                            .ToListAsync();
        }

        // GET: api/events/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var raceEvent = await _db.Events
                                     .Include(e => e.Organiser)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent is null)
                return NotFound(new { message = $"Event {id} was not found." });

            return raceEvent;
        }

        // POST: api/events
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent)
        {
            if (!ValidTypes.Contains(newEvent.EventType))
                return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

            if (!await _db.Users.AnyAsync(u => u.UserID == newEvent.OrganiserID))
                return BadRequest(new { message = $"Organiser {newEvent.OrganiserID} does not exist." });

            newEvent.EventID = 0;                 // let the store assign it
            newEvent.CreatedAt = DateTime.Now;
            newEvent.Organiser = null;            // don't re-insert the organiser

            _db.Events.Add(newEvent);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventID }, newEvent);
        }

        // PUT: api/events/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvent(int id, Event updated)
        {
            if (id != updated.EventID)
                return BadRequest(new { message = "Route id and body EventID do not match." });

            if (!ValidTypes.Contains(updated.EventType))
                return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

            var existing = await _db.Events.FindAsync(id);
            if (existing is null)
                return NotFound(new { message = $"Event {id} was not found." });

            existing.OrganiserID = updated.OrganiserID;
            existing.EventName = updated.EventName;
            existing.Description = updated.Description;
            existing.EventDate = updated.EventDate;
            existing.Location = updated.Location;
            existing.DistanceKm = updated.DistanceKm;
            existing.EventType = updated.EventType;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/events/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var raceEvent = await _db.Events
                                     .Include(e => e.Categories)
                                     .Include(e => e.Routes)
                                     .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent is null)
                return NotFound(new { message = $"Event {id} was not found." });

            var categoryIds = raceEvent.Categories.Select(c => c.CategoryID).ToList();
            if (await _db.Enrolments.AnyAsync(en => categoryIds.Contains(en.CategoryID)))
                return Conflict(new { message = "Cannot delete: participants are already enrolled in this event." });

            _db.Events.Remove(raceEvent);   // Categories + Routes cascade
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/events/5/categories
        [HttpGet("{id:int}/categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetEventCategories(int id)
        {
            if (!await _db.Events.AnyAsync(e => e.EventID == id))
                return NotFound(new { message = $"Event {id} was not found." });

            return await _db.Categories
                            .Where(c => c.EventID == id)
                            .AsNoTracking()
                            .ToListAsync();
        }

        // GET: api/events/5/routes
        [HttpGet("{id:int}/routes")]
        public async Task<ActionResult<IEnumerable<RaceRoute>>> GetEventRoutes(int id)
        {
            if (!await _db.Events.AnyAsync(e => e.EventID == id))
                return NotFound(new { message = $"Event {id} was not found." });

            return await _db.Routes
                            .Where(r => r.EventID == id)
                            .AsNoTracking()
                            .ToListAsync();
        }
    }
}
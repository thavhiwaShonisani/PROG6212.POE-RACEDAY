using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Helpers;
using RaceDayManagementAPI.Models;

namespace RaceDayManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public RoutesController(RaceDayDbContext db) => _db = db;

        private int CurrentUserId => User.GetUserId();

        // GET: api/routes  or  api/routes?eventId=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RaceRoute>>> GetRoutes([FromQuery] int? eventId)
        {
            var query = _db.Routes.AsNoTracking();

            if (eventId.HasValue)
                query = query.Where(r => r.EventID == eventId.Value);

            return await query.ToListAsync();
        }

        // GET: api/routes/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RaceRoute>> GetRoute(int id)
        {
            var route = await _db.Routes
                                 .AsNoTracking()
                                 .Include(r => r.Event)
                                 .FirstOrDefaultAsync(r => r.RouteID == id);

            if (route is null)
                return NotFound(new { message = $"Route {id} was not found." });

            return route;
        }

        // POST: api/routes
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<RaceRoute>> CreateRoute(RaceRoute newRoute)
        {
            var parent = await _db.Events.FindAsync(newRoute.EventID);
            if (parent is null)
                return BadRequest(new { message = $"Event {newRoute.EventID} does not exist." });

            if (parent.OrganiserID != CurrentUserId)
                return StatusCode(403, new { message = "You can only add routes to your own events." });

            newRoute.RouteID = 0;
            newRoute.Event = null;

            _db.Routes.Add(newRoute);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoute), new { id = newRoute.RouteID }, newRoute);
        }

        // PUT: api/routes/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateRoute(int id, RaceRoute updated)
        {
            if (id != updated.RouteID) return BadRequest(new { message = "Id mismatch." });

            var existing = await _db.Routes
                                    .Include(r => r.Event)
                                    .FirstOrDefaultAsync(r => r.RouteID == id);

            if (existing is null) return NotFound(new { message = $"Route {id} was not found." });
            if (existing.Event!.OrganiserID != CurrentUserId)
                return StatusCode(403, new { message = "You can only edit routes for your own events." });

            existing.RouteName = updated.RouteName;
            existing.DistanceKm = updated.DistanceKm;
            existing.MapFileUrl = updated.MapFileUrl;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/routes/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _db.Routes
                                 .Include(r => r.Event)
                                 .FirstOrDefaultAsync(r => r.RouteID == id);

            if (route is null) return NotFound(new { message = $"Route {id} was not found." });
            if (route.Event!.OrganiserID != CurrentUserId)
                return StatusCode(403, new { message = "You can only delete routes for your own events." });

            _db.Routes.Remove(route);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

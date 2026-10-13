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
    public class ResultsController : ControllerBase
    {
        private static readonly string[] ValidStatuses = { "Finished", "DNF", "DQ" };

        private readonly RaceDayDbContext _db;
        public ResultsController(RaceDayDbContext db) => _db = db;

        private int CurrentUserId => User.GetUserId();

        // GET: api/results
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Result>>> GetResults()
        {
            return await _db.Results
                            .AsNoTracking()
                            .ToListAsync();
        }

        // GET: api/results/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Result>> GetResult(int id)
        {
            var result = await _db.Results
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result is null)
                return NotFound(new { message = $"Result {id} was not found." });

            return result;
        }

        // POST: api/results — Organiser only, must own the parent event
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Result>> CreateResult(Result newResult)
        {
            if (!ValidStatuses.Contains(newResult.Status))
                return BadRequest(new { message = "Status must be Finished, DNF or DQ." });

            var enrolment = await _db.Enrolments
                                     .Include(e => e.Category)
                                     .FirstOrDefaultAsync(e => e.EnrolmentID == newResult.EnrolmentID);

            if (enrolment is null)
                return BadRequest(new { message = $"Enrolment {newResult.EnrolmentID} does not exist." });

            var eventOrganiserId = await _db.Events
                .Where(ev => ev.EventID == enrolment.Category!.EventID)
                .Select(ev => ev.OrganiserID)
                .FirstAsync();

            if (eventOrganiserId != CurrentUserId)
                return StatusCode(403, new { message = "You can only record results for your own events." });

            if (await _db.Results.AnyAsync(r => r.EnrolmentID == newResult.EnrolmentID))
                return Conflict(new { message = "A result already exists for that enrolment." });

            newResult.ResultID = 0;
            newResult.Enrolment = null;

            _db.Results.Add(newResult);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetResult), new { id = newResult.ResultID }, newResult);
        }

        // PUT: api/results/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateResult(int id, Result updated)
        {
            if (id != updated.ResultID) return BadRequest(new { message = "Id mismatch." });
            if (!ValidStatuses.Contains(updated.Status))
                return BadRequest(new { message = "Status must be Finished, DNF or DQ." });

            var existing = await _db.Results.FindAsync(id);
            if (existing is null) return NotFound(new { message = $"Result {id} was not found." });

            var eventOrganiserId = await _db.Enrolments
                .Where(e => e.EnrolmentID == existing.EnrolmentID)
                .Select(e => e.Category!.Event!.OrganiserID)
                .FirstAsync();

            if (eventOrganiserId != CurrentUserId)
                return StatusCode(403, new { message = "You can only edit results for your own events." });

            existing.FinishTime = updated.FinishTime;
            existing.Position = updated.Position;
            existing.Status = updated.Status;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/results/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var existing = await _db.Results.FindAsync(id);
            if (existing is null) return NotFound(new { message = $"Result {id} was not found." });

            var eventOrganiserId = await _db.Enrolments
                .Where(e => e.EnrolmentID == existing.EnrolmentID)
                .Select(e => e.Category!.Event!.OrganiserID)
                .FirstAsync();

            if (eventOrganiserId != CurrentUserId)
                return StatusCode(403, new { message = "You can only delete results for your own events." });

            _db.Results.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
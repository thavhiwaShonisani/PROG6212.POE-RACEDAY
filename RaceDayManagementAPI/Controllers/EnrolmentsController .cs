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
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public EnrolmentsController(RaceDayDbContext db) => _db = db;
        private int CurrentUserId => User.GetUserId();
        private string CurrentRole => User.GetRole();

        // GET: api/enrolments
        // Participant → only their own; Organiser → all enrolments in their events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Enrolment>>> GetEnrolments()
        {
            var query = _db.Enrolments
                           .Include(e => e.Participant)
                           .Include(e => e.Category)
                           .AsNoTracking();

            if (CurrentRole == "Participant")
                query = query.Where(e => e.ParticipantID == CurrentUserId);
            else
                query = query.Where(e => e.Category!.Event!.OrganiserID == CurrentUserId);

            return await query.ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Enrolment>> GetEnrolment(int id)
        {
            var enrolment = await _db.Enrolments
                                     .Include(e => e.Participant)
                                     .Include(e => e.Category)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment is null) return NotFound(new { message = $"Enrolment {id} was not found." });

            bool allowed =
                (CurrentRole == "Participant" && enrolment.ParticipantID == CurrentUserId) ||
                (CurrentRole == "Organiser" && await _db.Categories
                    .Where(c => c.CategoryID == enrolment.CategoryID)
                    .Select(c => c.Event!.OrganiserID)
                    .FirstAsync() == CurrentUserId);

            if (!allowed) return StatusCode(403, new { message = "You cannot view this enrolment." });

            return enrolment;
        }

        // POST: api/enrolments — Participant only
        [HttpPost]
        public async Task<ActionResult<Enrolment>> CreateEnrolment(Enrolment newEnrolment)
        {
            if (!await _db.Categories.AnyAsync(c => c.CategoryID == newEnrolment.CategoryID))
                return BadRequest(new { message = "Category does not exist." });

            bool duplicate = await _db.Enrolments.AnyAsync(e =>
                e.ParticipantID == CurrentUserId &&
                e.CategoryID == newEnrolment.CategoryID);
            if (duplicate)
                return Conflict(new { message = "You are already enrolled in that category." });

            newEnrolment.EnrolmentID = 0;
            newEnrolment.ParticipantID = CurrentUserId;   // ignore body, use session
            newEnrolment.EnrolmentDate = DateTime.Today;
            newEnrolment.Status = "Active";
            newEnrolment.Participant = null;
            newEnrolment.Category = null;

            _db.Enrolments.Add(newEnrolment);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = newEnrolment.EnrolmentID }, newEnrolment);
        }

        // PUT: api/enrolments/5 — Participant only, own enrolment only
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEnrolment(int id, Enrolment updated)
        {
            if (id != updated.EnrolmentID) return BadRequest(new { message = "Id mismatch." });

            var existing = await _db.Enrolments.FindAsync(id);
            if (existing is null) return NotFound(new { message = $"Enrolment {id} was not found." });
            if (existing.ParticipantID != CurrentUserId)
                return StatusCode(403, new { message = "You can only modify your own enrolments." });

            if (!new[] { "Active", "Cancelled" }.Contains(updated.Status))
                return BadRequest(new { message = "Status must be Active or Cancelled." });

            existing.Status = updated.Status;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            var existing = await _db.Enrolments.FindAsync(id);
            if (existing is null) return NotFound(new { message = $"Enrolment {id} was not found." });
            if (existing.ParticipantID != CurrentUserId)
                return StatusCode(403, new { message = "You can only cancel your own enrolments." });

            _db.Enrolments.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

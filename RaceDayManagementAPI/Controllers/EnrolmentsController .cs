using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Models;

namespace RaceDayManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public EnrolmentsController(RaceDayDbContext db) => _db = db;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Enrolment>> GetEnrolment(int id)
        {
            var enrolment = await _db.Enrolments.AsNoTracking()
                                               .Include(c => c.Status)
                                               .FirstOrDefaultAsync(c => c.EnrolmentID == id);

            if (enrolment is null) return NotFound(new { message = $"Enrolment {id} was not found." });
            return enrolment;
        }

        [HttpPost]
        public async Task<ActionResult<Enrolment>> CreateEnrolment(Enrolment newEnrolment)
        {

        var participant = await _db.Users.FindAsync(newEnrolment.ParticipantID);
            if (participant is null)
                return BadRequest(new { message = "Participant does not exist." });
            if (participant.Role != "Participant")
                return BadRequest(new { message = "Only users with the Participant role can enrol." });

            if (!await _db.Categories.AnyAsync(c => c.CategoryID == newEnrolment.CategoryID))
                return BadRequest(new { message = "Category does not exist." });

            if (!new[] { "Active", "Cancelled" }.Contains(newEnrolment.Status))
                return BadRequest(new { message = "Status must be Active or Cancelled." });

            bool duplicate = await _db.Enrolments.AnyAsync(e =>
                e.ParticipantID == newEnrolment.ParticipantID &&
                e.CategoryID == newEnrolment.CategoryID);

            if (duplicate)
                return Conflict(new { message = "This participant is already enrolled in that category." });

            newEnrolment.EnrolmentID = 0;
            newEnrolment.EnrolmentDate = DateTime.Today;
            newEnrolment.Participant = null;
            newEnrolment.Category = null;

            _db.Enrolments.Add(newEnrolment);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = newEnrolment.EnrolmentID }, newEnrolment);
        }
    }
}

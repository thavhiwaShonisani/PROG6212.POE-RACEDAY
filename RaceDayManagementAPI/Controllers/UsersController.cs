using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace RaceDayManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly string[] ValidRoles = { "Organiser", "Participant" };

        private readonly RaceDayDbContext _db;
        public UsersController(RaceDayDbContext db) => _db = db;

        // GET: api/users?role=Organiser
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] string? role)
        {
            var query = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            return await query.ToListAsync();
        }

        // GET: api/users/3
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _db.Users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.UserID == id);

            if (user is null) return NotFound(new { message = $"User {id} was not found." });
            return user;
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User newUser)
        {
            if (!ValidRoles.Contains(newUser.Role))
                return BadRequest(new { message = "Role must be Organiser or Participant." });

            if (await _db.Users.AnyAsync(u => u.Email == newUser.Email))
                return Conflict(new { message = "That email address is already registered." });

            newUser.UserID = 0;
            newUser.CreatedAt = DateTime.Now;
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.PasswordHash);

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = newUser.UserID }, newUser);
        }

        // PUT: api/users/3
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, User updated)
        {
            if (id != updated.UserID)
                return BadRequest(new { message = "Route id and body UserID do not match." });

            if (!ValidRoles.Contains(updated.Role))
                return BadRequest(new { message = "Role must be Organiser or Participant." });

            var existing = await _db.Users.FindAsync(id);
            if (existing is null) return NotFound(new { message = $"User {id} was not found." });

            if (await _db.Users.AnyAsync(u => u.Email == updated.Email && u.UserID != id))
                return Conflict(new { message = "That email address is already registered." });

            existing.FullName = updated.FullName;
            existing.Email = updated.Email;
            existing.Role = updated.Role;

            // Only replace the hash if a new one was supplied
            if (!string.IsNullOrWhiteSpace(updated.PasswordHash))
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updated.PasswordHash);

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/users/3
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound(new { message = $"User {id} was not found." });

            if (await _db.Events.AnyAsync(e => e.OrganiserID == id))
                return Conflict(new { message = "Cannot delete: this user still organises events." });

            if (await _db.Enrolments.AnyAsync(en => en.ParticipantID == id))
                return Conflict(new { message = "Cannot delete: this user has enrolments." });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
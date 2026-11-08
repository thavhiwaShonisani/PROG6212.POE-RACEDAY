using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Models;

namespace RaceDayManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
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
    }
}
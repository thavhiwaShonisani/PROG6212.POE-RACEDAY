using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Helpers;
using RaceDayManagementAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaceDayManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(RaceDayDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(User registration)
        {
            if (!new[] { "Organiser", "Participant" }
                    .Contains(registration.Role, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { message = "Role must be Organiser or Participant." });

            if (string.IsNullOrWhiteSpace(registration.PasswordHash) ||
                registration.PasswordHash.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters." });

            if (await _db.Users.AnyAsync(u => u.Email == registration.Email))
                return Conflict(new { message = "That email address is already registered." });

            var user = new User
            {
                FullName = registration.FullName,
                Email = registration.Email,
                Role = char.ToUpper(registration.Role[0]) + registration.Role[1..].ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registration.PasswordHash),
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = GenerateToken(user);

            return Ok(new
            {
                message = "Registered.",
                token,
                user = new { user.UserID, user.FullName, user.Email, user.Role }
            });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(User credentials)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == credentials.Email);

            if (user is null || !BCrypt.Net.BCrypt.Verify(credentials.PasswordHash, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            var token = GenerateToken(user);

            return Ok(new
            {
                message = "Logged in.",
                token,
                user = new { user.UserID, user.FullName, user.Email, user.Role }
            });
        }

        // GET: api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var id = User.GetUserId();
            var user = await _db.Users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.UserID == id);
            if (user is null) return NotFound();

            return Ok(new { user.UserID, user.FullName, user.Email, user.Role, user.CreatedAt });
        }

        // No /logout endpoint needed — the client just discards the token.
        // If you want one for symmetry, it's a no-op that returns 200.

        private string GenerateToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub,   user.UserID.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier,     user.UserID.ToString()),
            new Claim(ClaimTypes.Name,               user.FullName),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
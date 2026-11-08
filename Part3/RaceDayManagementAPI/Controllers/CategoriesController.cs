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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public CategoriesController(RaceDayDbContext db) => _db = db;

        private int CurrentUserId => User.GetUserId();

        // GET: api/categories  or  api/categories?eventId=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] int? eventId)
        {
            var query = _db.Categories.AsNoTracking();

            if (eventId.HasValue)
                query = query.Where(c => c.EventID == eventId.Value);

            return await query.ToListAsync();
        }

        // GET: api/categories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _db.Categories
                                    .AsNoTracking()
                                    .Include(c => c.Event)
                                    .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category is null)
                return NotFound(new { message = $"Category {id} was not found." });

            return category;
        }

        // POST: api/categories
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Category>> CreateCategory(Category newCategory)
        {
            var parent = await _db.Events.FindAsync(newCategory.EventID);
            if (parent is null)
                return BadRequest(new { message = $"Event {newCategory.EventID} does not exist." });

            if (parent.OrganiserID != CurrentUserId)
                return StatusCode(403, new { message = "You can only add categories to your own events." });

            if (newCategory.EntryFee < 0)
                return BadRequest(new { message = "EntryFee cannot be negative." });

            newCategory.CategoryID = 0;
            newCategory.Event = null;

            _db.Categories.Add(newCategory);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = newCategory.CategoryID }, newCategory);
        }

        // PUT: api/categories/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, Category updated)
        {
            if (id != updated.CategoryID) return BadRequest(new { message = "Id mismatch." });

            var existing = await _db.Categories
                                    .Include(c => c.Event)
                                    .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (existing is null) return NotFound(new { message = $"Category {id} was not found." });
            if (existing.Event!.OrganiserID != CurrentUserId)
                return StatusCode(403, new { message = "You can only edit categories for your own events." });

            existing.CategoryName = updated.CategoryName;
            existing.DistanceKm = updated.DistanceKm;
            existing.EntryFee = updated.EntryFee;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.Categories
                                    .Include(c => c.Event)
                                    .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category is null) return NotFound(new { message = $"Category {id} was not found." });
            if (category.Event!.OrganiserID != CurrentUserId)
                return StatusCode(403, new { message = "You can only delete categories for your own events." });
            if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id))
                return Conflict(new { message = "Cannot delete: there are enrolments for this category." });

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
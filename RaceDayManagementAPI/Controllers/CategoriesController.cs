using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayManagementAPI.Data;
using RaceDayManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace RaceDayManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        public CategoriesController(RaceDayDbContext db) => _db = db;

        // GET: api/categories  or  api/categories?eventId=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] int? eventId)
        {
            var query = _db.Categories.AsNoTracking();

            if (eventId.HasValue)
                query = query.Where(c => c.EventID == eventId.Value);

            return await query.ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _db.Categories.AsNoTracking()
                                               .Include(c => c.Event)
                                               .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category is null) return NotFound(new { message = $"Category {id} was not found." });
            return category;
        }

        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category newCategory)
        {
            if (!await _db.Events.AnyAsync(e => e.EventID == newCategory.EventID))
                return BadRequest(new { message = $"Event {newCategory.EventID} does not exist." });

            if (newCategory.EntryFee < 0)
                return BadRequest(new { message = "EntryFee cannot be negative." });

            newCategory.CategoryID = 0;
            newCategory.Event = null;

            _db.Categories.Add(newCategory);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = newCategory.CategoryID }, newCategory);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, Category updated)
        {
            if (id != updated.CategoryID) return BadRequest(new { message = "Id mismatch." });

            var existing = await _db.Categories.FindAsync(id);
            if (existing is null) return NotFound(new { message = $"Category {id} was not found." });

            existing.CategoryName = updated.CategoryName;
            existing.DistanceKm = updated.DistanceKm;
            existing.EntryFee = updated.EntryFee;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category is null) return NotFound(new { message = $"Category {id} was not found." });

            if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id))
                return Conflict(new { message = "Cannot delete: there are enrolments for this category." });

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
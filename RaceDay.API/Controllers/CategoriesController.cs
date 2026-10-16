using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // GET: api/categories/event/1
        // Both Participants and Organisers can view categories.
        [HttpGet("event/{eventId:int}")]
        public async Task<IActionResult> GetCategoriesByEvent(int eventId)
        {
            var eventExists = await _context.Events
                .AsNoTracking()
                .AnyAsync(e => e.EventId == eventId);

            if (!eventExists)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.CategoryName)
                .Select(c => new
                {
                    c.CategoryId,
                    c.EventId,
                    c.CategoryName,
                    c.MinAge,
                    c.MaxAge,
                    c.DistanceKm
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/categories
        // Only the organiser who owns the event can add categories.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == category.EventId);

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (existingEvent.OrganiserId != userId)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return BadRequest(new
                {
                    message = "Category name is required."
                });
            }

            if (category.MinAge < 0 ||
                category.MaxAge < category.MinAge)
            {
                return BadRequest(new
                {
                    message = "The category age range is invalid."
                });
            }

            if (category.DistanceKm <= 0)
            {
                return BadRequest(new
                {
                    message = "Distance must be greater than zero."
                });
            }

            var duplicateCategory = await _context.Categories
                .AnyAsync(c =>
                    c.EventId == category.EventId &&
                    c.CategoryName == category.CategoryName);

            if (duplicateCategory)
            {
                return Conflict(new
                {
                    message = "A category with this name already exists for the event."
                });
            }

            category.CategoryId = 0;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategoriesByEvent),
                new { eventId = category.EventId },
                new
                {
                    category.CategoryId,
                    category.EventId,
                    category.CategoryName,
                    category.MinAge,
                    category.MaxAge,
                    category.DistanceKm
                });
        }

        // DELETE: api/categories/1
        // Only the organiser who owns the event can delete the category.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Event.OrganiserId != userId)
            {
                return Forbid();
            }

            var hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.CategoryId == id);

            if (hasEnrolments)
            {
                return Conflict(new
                {
                    message = "This category cannot be deleted because it has enrolments."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}